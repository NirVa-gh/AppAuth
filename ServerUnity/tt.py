import time

phases = [("red", 7),
            ("red+yellow", 2),
            ("green", 5),
            ("yellow", 2)]

current_phase = 0
while True:
    time.sleep(1)
    current_phase = (current_phase + 1) % len(phases)
    print(current_phase)