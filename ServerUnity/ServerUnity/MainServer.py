import os
from flask import Flask, request, jsonify
import sqlite3
import bcrypt
import jwt
from datetime import datetime, timedelta, timezone
from functools import wraps

app = Flask(__name__)
app.config['SECRET_KEY'] = '555'


# ======================
# Вспомогательные функции
# ======================

def init_db():
    """Создает таблицы в базе данных при первом запуске"""
    conn = sqlite3.connect('users.db')
    cursor = conn.cursor()

    # Таблица пользователей
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT UNIQUE NOT NULL,
        email TEXT UNIQUE NOT NULL,
        password TEXT NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        is_partner INTEGER NOT NULL DEFAULT 0
    )
    ''')

    # Таблица заявок
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS requests (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        title TEXT NOT NULL,
        content TEXT NOT NULL,
        status TEXT DEFAULT 'new',
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        user_id INTEGER REFERENCES users(id)
    )
    ''')

    conn.commit()
    conn.close()

def hash_password(password):
    """Хеширует пароль с солью"""
    return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')

def check_password(hashed, password):
    """Проверяет пароль"""
    return bcrypt.checkpw(password.encode('utf-8'), hashed.encode('utf-8'))

def create_token(username):
    """Генерирует JWT токен"""
    return jwt.encode(payload={
        'username': username,
        'exp': datetime.now(timezone.utc) + timedelta(hours=24)
    }, key= app.config['SECRET_KEY'], algorithm='HS256')

def get_user_id(username):
    """Получает ID пользователя по имени"""
    conn = sqlite3.connect('users.db')
    cursor = conn.cursor()
    cursor.execute('SELECT id FROM users WHERE username = ?', (username,))
    user = cursor.fetchone()
    conn.close()
    return user[0] if user else None

# ======================
# Эндпоинты аутентификации
# ======================
@app.route('/api/register', methods=['POST'])
def register():
    """
    Регистрация нового пользователя
    Возможные коды ответов:
    - 201: Успешная регистрация
    - 400: Неверные данные
    - 409: Конфликт (пользователь уже существует)
    - 415: Неподдерживаемый тип данных
    - 500: Ошибка сервера
    """
    response = {
        'success': False,
        'message': '',
        'user_id': None
    }

    # Проверка Content-Type
    if not request.is_json and request.content_type not in ['multipart/form-data', 'application/x-www-form-urlencoded']:
        response['message'] = 'Unsupported Media Type: требуется application/json или form-data'
        return jsonify(response), 415

    try:
        # Парсинг данных
        try:
            data = request.get_json() if request.is_json else request.form
        except Exception as e:
            response['message'] = f'Ошибка парсинга данных: {str(e)}'
            return jsonify(response), 400

        # Валидация полей
        username = data.get('username', '').strip()
        email = data.get('email', '').strip()
        password = data.get('password', '').strip()

        if not all([username, email, password]):
            response['message'] = 'Все поля обязательны (username, email, password)'
            return jsonify(response), 400

        if len(password) < 6:
            response['message'] = 'Пароль должен содержать минимум 6 символов'
            return jsonify(response), 400

        # Подключение к БД с обработкой блокировок
        conn = None
        try:
            conn = sqlite3.connect('users.db', timeout=30)
            conn.execute("PRAGMA journal_mode=WAL")
            conn.execute("PRAGMA busy_timeout=30000")
            cursor = conn.cursor()

            # Проверка существующего пользователя
            cursor.execute(
                'SELECT id FROM users WHERE username = ? OR email = ? LIMIT 1',
                (username, email)
            )
            if cursor.fetchone():
                response['message'] = 'Пользователь с таким именем или email уже существует'
                return jsonify(response), 409

            # Хеширование пароля
            try:
                hashed_pw = bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')
            except Exception as e:
                response['message'] = f'Ошибка хеширования пароля: {str(e)}'
                return jsonify(response), 500

            # Создание пользователя
            cursor.execute(
                'INSERT INTO users (username, email, password) VALUES (?, ?, ?)',
                (username, email, hashed_pw)
            )
            user_id = cursor.lastrowid
            conn.commit()

            # Генерация токена
            try:
                token = jwt.encode(
                    {
                        'user_id': user_id,
                        'username': username,
                        'exp': datetime.utcnow() + timedelta(hours=24)
                    },
                    app.config['SECRET_KEY'],
                    algorithm='HS256'
                )
            except Exception as e:
                response['message'] = f'Ошибка генерации токена: {str(e)}'
                return jsonify(response), 500

            # Успешный ответ
            response.update({
                'success': True,
                'message': 'Регистрация успешно завершена',
                'token': token,
                'user_id': user_id
            })
            return jsonify(response), 201

        except sqlite3.OperationalError as e:
            conn.rollback()
            response['message'] = f'Ошибка базы данных: {str(e)}'
            return jsonify(response), 500
        except sqlite3.IntegrityError as e:
            conn.rollback()
            response['message'] = 'Конфликт данных: возможно, пользователь уже существует'
            return jsonify(response), 409
        except Exception as e:
            conn.rollback()
            response['message'] = f'Неизвестная ошибка базы данных: {str(e)}'
            return jsonify(response), 500
        finally:
            if conn:
                conn.close()

    except Exception as e:
        response['message'] = f'Критическая ошибка сервера: {str(e)}'
        return jsonify(response), 500

@app.route('/api/login', methods=['POST'])
def login():
    if request.content_type not in ['application/json', 'multipart/form-data', 'application/x-www-form-urlencoded']:
        return jsonify({'success': False, 'message': 'Unsupported Media Type'}), 415

    if request.is_json:
        data = request.get_json()
    else:
        data = request.form

    username = data.get('username')
    password = data.get('password')

    if not username or not password:
        return jsonify({'success': False, 'message': 'Требуется имя пользователя и пароль'}), 400

    conn = None
    try:
        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Выбираем все необходимые поля, включая is_partner
        cursor.execute(
            'SELECT id, password, is_partner FROM users WHERE username = ?',
            (username,)
        )
        user = cursor.fetchone()

        if not user:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        user_id = user[0]
        stored_password = user[1]
        is_partner = user[2]  # Получаем значение is_partner

        if check_password(stored_password, password):
            token = create_token(username)
            print(jsonify({
                'success': True,
                'message': 'Вход выполнен',
                'token': token,
                'user_id': user_id,
                'is_partner': is_partner  # Исправлено название поля
            }))
            return jsonify({
                'success': True,
                'message': 'Вход выполнен',
                'token': token,
                'user_id': user_id,
                'is_partner': is_partner  # Исправлено название поля
            })
        else:
            return jsonify({'success': False, 'message': 'Неверный пароль'}), 401

    except Exception as e:
        return jsonify({'success': False, 'message': str(e)}), 500
    finally:
        if conn:
            conn.close()

# ======================
# Эндпоинты для заявок
# ======================

@app.route('/api/requests', methods=['POST'])
def create_request():
    conn = None
    """Создание новой заявки"""
    token = request.headers.get('Authorization')
    if not token:
        return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

    try:
        decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        user_id = get_user_id(decoded['username'])

        data = request.json
        title = data.get('title')
        content = data.get('content')
        status = data.get('status', 'new')

        if not all([title, content]):
            return jsonify({'success': False, 'message': 'Заполните все поля'}), 400

        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Вставляем новую заявку
        cursor.execute(
            'INSERT INTO requests (title, content, status, user_id) VALUES (?, ?, ?, ?)',
            (data['title'], data['content'], data.get('status', 'new'), user_id)
        )
        conn.commit()

        # Получаем ID ВСТАВЛЕННОЙ записи
        new_id = cursor.lastrowid

        if not new_id:  # Если ID не получен
            conn.rollback()
            return jsonify({
                'success': False,
                'message': 'Не удалось получить ID заявки'
            }), 500

        # Возвращаем данные с ID
        return jsonify({
            'success': True,
            'message': 'Заявка создана',
            'request': {
                'id': new_id,  # Гарантированно числовой ID > 0
                'title': data['title'],
                'content': data['content'],
                'status': data.get('status', 'new'),
                'user_id': user_id
            }
        }), 201

    except Exception as e:
        if conn:
            conn.rollback()
        return jsonify({'success': False, 'message': str(e)}), 500
    finally:
        if conn:
            conn.close()

@app.route('/api/requests/<int:request_id>', methods=['GET'])
def get_single_request(request_id):
    """Получение конкретной заявки по ID"""
    token = request.headers.get('Authorization')
    if not token:
        return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

    conn = None
    try:
        decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        user_id = get_user_id(decoded['username'])
        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        cursor.execute(
            '''SELECT id, title, content, status, 
               strftime('%Y-%m-%d %H:%M:%S', created_at) 
               FROM requests WHERE id = ?''',
            (request_id,)
        )
        db_request = cursor.fetchone()

        if not db_request:
            print(request_id, user_id)
            return jsonify({'success': False, 'message': f'Заявка не найдена '}), 404

        # Формируем ответ
        response_data = {
            'request': {
                'id': db_request[0],
                'title': db_request[1],
                'content': db_request[2],
                'status': db_request[3],
                'created_at': db_request[4]
            },
            'success': True,
        }
        return jsonify(response_data), 200

    except jwt.ExpiredSignatureError:
        return jsonify({'success': False, 'message': 'Срок действия токена истёк'}), 401
    except jwt.InvalidTokenError:
        return jsonify({'success': False, 'message': 'Недействительный токен'}), 401
    except sqlite3.Error as e:
        return jsonify({'success': False, 'message': f'Ошибка базы данных: {str(e)}'}), 500
    except Exception as e:
        return jsonify({'success': False, 'message': f'Неизвестная ошибка: {str(e)}'}), 500
    finally:
        if conn:
            conn.close()

@app.route('/api/requestsByUserID', methods=['GET'])
def get_requests_by_user_ID():
    try:
        # Проверка авторизации
        token = request.headers.get('Authorization')
        if not token:
            return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

        # Получаем user_id из токена
        decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        user_id = get_user_id(decoded['username'])

        if not user_id:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        # Получаем параметры запроса
        request_user_id = request.args.get('user_id')
        if request_user_id and int(request_user_id) != user_id:
            return jsonify({'success': False, 'message': 'Недостаточно прав'}), 403

        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Запрос заявок
        query = '''
            SELECT id, title, content, status, 
                   strftime('%Y-%m-%d %H:%M:%S', created_at) 
            FROM requests 
            WHERE user_id = ?
            ORDER BY created_at DESC
        '''
        cursor.execute(query, (user_id,))

        requests = []
        for row in cursor.fetchall():
            requests.append({
                'id': row[0],
                'title': row[1],
                'content': row[2],
                'status': row[3],
                'created_at': row[4]
            })

        return jsonify({
            'success': True,
            'requests': requests
        })

    except Exception as e:
        return jsonify({'success': False, 'message': str(e)}), 500
    finally:
        if conn:
            conn.close()

@app.route('/api/requests/<int:request_id>', methods=['PUT'])
def update_request(request_id):
    try:
        # Проверка токена
        token = request.headers.get('Authorization')
        if not token:
            return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

        try:
            decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        except Exception as e:
            return jsonify({'success': False, 'message': f'Ошибка токена: {str(e)}'}), 401

        # Проверка Content-Type
        if not request.is_json:
            return jsonify({'success': False, 'message': 'Требуется application/json'}), 400

        data = request.get_json()
        print("Received data:", data)  # Лог для отладки

        # Валидация полей
        required_fields = ['title', 'content']
        if not all(field in data for field in required_fields):
            return jsonify({
                'success': False,
                'message': f'Не хватает полей: {", ".join(required_fields)}'
            }), 400

        if not data['title'] or not data['content']:
            return jsonify({'success': False, 'message': 'Поля не могут быть пустыми'}), 400

        # Обновление в БД
        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        try:
            cursor.execute('''
                UPDATE requests 
                SET title = ?, content = ?
                WHERE id = ?
            ''', (data['title'], data['content'], request_id))
            conn.commit()
        except sqlite3.Error as e:
            conn.rollback()
            return jsonify({'success': False, 'message': f'Ошибка БД: {str(e)}'}), 500
        finally:
            conn.close()

        return jsonify({
            'success': True,
            'message': 'Заявка обновлена',
            'request': {
                'id': request_id,
                'title': data['title'],
                'content': data['content']
            }
        })

    except Exception as e:
        return jsonify({'success': False, 'message': f'Ошибка сервера: {str(e)}'}), 500


@app.route('/api/requests/<int:request_id>', methods=['DELETE'])
def delete_request(request_id):
    """Удаление заявки"""
    token = request.headers.get('Authorization')
    if not token:
        return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

    conn = None
    try:
        # Проверяем токен
        try:
            decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        except Exception as e:
            return jsonify({'success': False, 'message': f'Ошибка токена: {str(e)}'}), 401

        user_id = get_user_id(decoded['username'])
        if not user_id:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Проверяем, существует ли заявка и принадлежит ли пользователю
        cursor.execute('SELECT user_id FROM requests WHERE id = ?', (request_id,))
        request_data = cursor.fetchone()

        if not request_data:
            return jsonify({'success': False, 'message': 'Заявка не найдена'}), 404

        if request_data[0] != user_id :
            print(user_id)
            return jsonify({'success': False, 'message': 'Недостаточно прав'}), 403 ## request_data[0] != user_id???

        # Удаляем заявку
        cursor.execute('DELETE FROM requests WHERE id = ?', (request_id,))
        conn.commit()

        return jsonify({'success': True, 'message': 'Заявка удалена'}), 200

    except sqlite3.Error as e:
        if conn:
            conn.rollback()
        return jsonify({'success': False, 'message': f'Ошибка базы данных: {str(e)}'}), 500
    except Exception as e:
        return jsonify({'success': False, 'message': f'Неизвестная ошибка: {str(e)}'}), 500
    finally:
        if conn:
            conn.close()

@app.route('/api/requestsAdmin/<int:request_id>', methods=['DELETE'])
def delete_request_admin(request_id):
    """Удаление заявки (администратором, если is_partner = 1)"""
    token = request.headers.get('Authorization')
    if not token:
        return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

    conn = None
    try:
        # Проверяем токен
        try:
            decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        except Exception as e:
            return jsonify({'success': False, 'message': f'Ошибка токена: {str(e)}'}), 401

        username = decoded.get('username')
        user_id = get_user_id(username)
        if not user_id:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Проверяем, является ли пользователь администратором (is_partner = 1)
        cursor.execute('SELECT is_partner FROM users WHERE id = ?', (user_id,))
        user_data = cursor.fetchone()

        if not user_data:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        if user_data[0] != 1:
            return jsonify({'success': False, 'message': 'Недостаточно прав (нужен администратор)'}), 403

        # Проверяем, существует ли заявка
        cursor.execute('SELECT id FROM requests WHERE id = ?', (request_id,))
        if not cursor.fetchone():
            return jsonify({'success': False, 'message': 'Заявка не найдена'}), 404

        # Удаляем заявку
        cursor.execute('DELETE FROM requests WHERE id = ?', (request_id,))
        conn.commit()

        return jsonify({'success': True, 'message': f'Заявка {request_id} удалена администратором'}), 200

    except sqlite3.Error as e:
        if conn:
            conn.rollback()
        return jsonify({'success': False, 'message': f'Ошибка базы данных: {str(e)}'}), 500
    except Exception as e:
        return jsonify({'success': False, 'message': f'Неизвестная ошибка: {str(e)}'}), 500
    finally:
        if conn:
            conn.close()

@app.route('/api/requests', methods=['GET'])
def get_all_requests():
    """Получение ВСЕХ заявок (без проверки токена и фильтрации)"""
    conn = None
    try:
        # Проверка существования файла БД
        db_path = 'users.db'
        if not os.path.exists(db_path):
            return jsonify({
                'success': False,
                'message': 'Database file not found'
            }), 500

        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()

        # Проверка существования таблицы
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='requests'")
        if not cursor.fetchone():
            return jsonify({
                'success': False,
                'message': 'Table "requests" does not exist'
            }), 500

        # Получение данных
        cursor.execute('''
            SELECT id, title, content, status, created_at
            FROM requests
            ORDER BY created_at DESC
        ''')

        # Форматирование результата
        columns = [desc[0] for desc in cursor.description]
        requests = [dict(zip(columns, row)) for row in cursor.fetchall()]

        return jsonify({
            'success': True,
            'requests': requests
        })

    except sqlite3.Error as e:
        return jsonify({
            'success': False,
            'message': f'Database error: {str(e)}'
        }), 500

    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Server error: {str(e)}'
        }), 500

    finally:
        if conn:
            conn.close()

@app.route('/api/request', methods=['GET'])
def get_requests():
    """Получение заявок с проверкой токена и фильтрацией по пользователю"""
    # Проверка авторизации
    auth_header = request.headers.get('Authorization')
    if not auth_header:
        return jsonify({'success': False, 'message': 'Authorization token is missing'}), 401

    try:
        # Извлечение и проверка токена
        token = auth_header.split()[1]  # Формат: Bearer <token>
        decoded_token = jwt.decode(token, app.config['SECRET_KEY'], algorithms=['HS256'])
        user_id = decoded_token.get('user_id')

        if not user_id:
            return jsonify({'success': False, 'message': 'Invalid token'}), 401

        # Подключение к БД
        db_path = 'users.db'
        if not os.path.exists(db_path):
            return jsonify({'success': False, 'message': 'Database file not found'}), 500

        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()

        # Проверка существования таблицы
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='requests'")
        if not cursor.fetchone():
            return jsonify({'success': False, 'message': 'Table "requests" does not exist'}), 500

        # Получение заявок для конкретного пользователя
        cursor.execute('''
            SELECT id, title, content, status, created_at
            FROM requests
            WHERE user_id = ?
            ORDER BY created_at DESC
        ''', (user_id,))

        # Форматирование результата
        columns = [desc[0] for desc in cursor.description]
        requests = [dict(zip(columns, row)) for row in cursor.fetchall()]

        return jsonify({
            'success': True,
            'requests': requests
        })

    except jwt.ExpiredSignatureError:
        return jsonify({'success': False, 'message': 'Token has expired'}), 401
    except jwt.InvalidTokenError:
        return jsonify({'success': False, 'message': 'Invalid token'}), 401
    except sqlite3.Error as e:
        return jsonify({'success': False, 'message': f'Database error: {str(e)}'}), 500
    except Exception as e:
        return jsonify({'success': False, 'message': f'Server error: {str(e)}'}), 500
    finally:
        if conn:
            conn.close()

# ======================
# Сверху работает
# ======================

@app.route('/api/requestsAdminAccept/<int:request_id>', methods=['PATCH'])
def update_request_status(request_id):
    """Изменение статуса заявки администратором"""
    token = request.headers.get('Authorization')
    if not token:
        return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

    # Получаем данные из тела запроса
    data = request.get_json()
    if not data or 'status' not in data:
        return jsonify({'success': False, 'message': 'Не указан статус'}), 400

    new_status = data['status']

    # Проверяем допустимые статусы
    allowed_statuses = ['Pending', 'Accepted', 'Rejected', 'Completed']
    if new_status not in allowed_statuses:
        return jsonify(
            {'success': False, 'message': f'Недопустимый статус. Допустимые: {", ".join(allowed_statuses)}'}), 400

    conn = None
    try:
        # Проверяем токен
        try:
            decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        except Exception as e:
            return jsonify({'success': False, 'message': f'Ошибка токена: {str(e)}'}), 401

        username = decoded.get('username')
        user_id = get_user_id(username)
        if not user_id:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Проверяем, является ли пользователь администратором (is_partner = 1)
        cursor.execute('SELECT is_partner FROM users WHERE id = ?', (user_id,))
        user_data = cursor.fetchone()

        if not user_data:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        if user_data[0] != 1:
            return jsonify({'success': False, 'message': 'Недостаточно прав (нужен администратор)'}), 403

        # Проверяем, существует ли заявка
        cursor.execute('SELECT id FROM requests WHERE id = ?', (request_id,))
        if not cursor.fetchone():
            return jsonify({'success': False, 'message': 'Заявка не найдена'}), 404

        # Обновляем статус заявки
        cursor.execute(
            'UPDATE requests SET status = ? WHERE id = ?',
            (new_status, request_id)
        )
        conn.commit()

        return jsonify({
            'success': True,
            'message': f'Статус заявки {request_id} изменен на "{new_status}"'
        }), 200

    except sqlite3.Error as e:
        if conn:
            conn.rollback()
        return jsonify({'success': False, 'message': f'Ошибка базы данных: {str(e)}'}), 500
    except Exception as e:
        return jsonify({'success': False, 'message': f'Неизвестная ошибка: {str(e)}'}), 500
    finally:
        if conn:
            conn.close()


@app.route('/api/requests/by-status/<status>', methods=['GET'])
def get_requests_by_status(status):
    """Получение заявок по статусу"""
    token = request.headers.get('Authorization')
    if not token:
        return jsonify({'success': False, 'message': 'Токен отсутствует'}), 401

    conn = None
    try:
        # Проверяем токен
        try:
            decoded = jwt.decode(token.split()[1], app.config['SECRET_KEY'], algorithms=['HS256'])
        except Exception as e:
            return jsonify({'success': False, 'message': f'Ошибка токена: {str(e)}'}), 401

        username = decoded.get('username')
        user_id = get_user_id(username)
        if not user_id:
            return jsonify({'success': False, 'message': 'Пользователь не найден'}), 404

        conn = sqlite3.connect('users.db')
        cursor = conn.cursor()

        # Проверяем права администратора
        cursor.execute('SELECT is_partner FROM users WHERE id = ?', (user_id,))
        user_data = cursor.fetchone()

        if not user_data or user_data[0] != 1:
            return jsonify({'success': False, 'message': 'Недостаточно прав'}), 403

        # Получаем заявки по статусу
        cursor.execute(
            '''SELECT id, title, content, status, 
               strftime('%Y-%m-%d %H:%M:%S', created_at) 
               FROM requests WHERE status = ?''',
            (status,)
        )

        requests = cursor.fetchall()
        result = []
        for req in requests:
            result.append({
                'id': req[0],
                'title': req[1],
                'content': req[2],
                'status': req[3],
                'created_at': req[4]
            })

        return jsonify({
            'success': True,
            'requests': result
        }), 200

    except sqlite3.Error as e:
        return jsonify({'success': False, 'message': f'Ошибка базы данных: {str(e)}'}), 500
    except Exception as e:
        return jsonify({'success': False, 'message': f'Неизвестная ошибка: {str(e)}'}), 500
    finally:
        if conn:
            conn.close()

# ======================
# Запуск сервера
# ======================

if __name__ == '__main__':
    init_db()  # Инициализация базы данных
    app.run(host='0.0.0.0', port=5000, debug=False)