using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UIWidgets
{
	
	[RequireComponent(typeof(CanvasGroup))]
	public class UIWidget : MonoBehaviour
	{


        private GameObject[] backpackTest = new GameObject[3];
        private int[] intTest = new int[3]; //(null,null,null)



        [Tooltip("Name of the widget.")]
		[SerializeField]
		protected new string name;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		[Header("Appearence")]
		[Tooltip("Key to show or close this widget.")]
		[SerializeField]
		protected KeyCode m_KeyCode = KeyCode.None;
		[Tooltip("Easing equation type used to tween this widget.")]
		[SerializeField]
		private EasingEquations.EaseType m_EaseType = EasingEquations.EaseType.EaseInOutBack;

		[Tooltip("The duration to tween this widget.")]
		[SerializeField]
		protected float m_Duration = 0.7f;

		[SerializeField]
		protected bool m_IgnoreTimeScale = true;
		public bool IgnoreTimeScale { get { return this.m_IgnoreTimeScale; } }

		[Tooltip("The AudioClip that will be played when this widget shows.")]
		[SerializeField]
        protected AudioClip m_ShowSound;


		[Tooltip("The AudioClip that will be played when this widget closes.")]
		[SerializeField]
		protected AudioClip m_CloseSound;
    	protected static CursorLockMode m_PreviousCursorLockMode;
		protected static bool m_PreviousCursorVisibility;
		protected Transform m_CameraTransform;
		protected MonoBehaviour m_CameraController;
		protected MonoBehaviour m_ThirdPersonController;
		protected static bool m_PreviousCameraControllerEnabled;
		protected static List<UIWidget> m_CurrentVisibleWidgets=new List<UIWidget>();

		public bool IsVisible { 
			get {
				if (this.m_CanvasGroup == null)
					this.m_CanvasGroup = GetComponent<CanvasGroup>();
				return m_CanvasGroup.alpha == 1f; 
			} 
		}

		protected RectTransform m_RectTransform;
		protected CanvasGroup m_CanvasGroup;
		protected bool m_IsShowing;
		private TweenRunner<FloatTween> m_AlphaTweenRunner;
		private TweenRunner<Vector3Tween> m_ScaleTweenRunner;

		protected Scrollbar[] m_Scrollbars;
		protected bool m_IsLocked = false;
		public bool IsLocked
		{
			get { return this.m_IsLocked; }
		}
		private void Awake ()
		{
			WidgetInputHandler.RegisterInput(this.m_KeyCode, this);
			m_RectTransform = GetComponent<RectTransform> ();
			m_CanvasGroup = GetComponent<CanvasGroup> ();
			this.m_Scrollbars = GetComponentsInChildren<Scrollbar>();
			this.m_CameraTransform = Camera.main.transform;
			if (!IsVisible) 
            {
				m_RectTransform.localScale = Vector3.zero;
			}
			if (this.m_AlphaTweenRunner == null)
				this.m_AlphaTweenRunner = new TweenRunner<FloatTween> ();
			this.m_AlphaTweenRunner.Init (this);

			if (this.m_ScaleTweenRunner == null)
				this.m_ScaleTweenRunner = new TweenRunner<Vector3Tween> ();
			this.m_ScaleTweenRunner.Init (this);
            m_IsShowing = IsVisible;

		}
		private void Start ()
		{
			OnStart ();
			StartCoroutine (OnDelayedStart ());
		}

		protected virtual void OnStart ()
		{
		}
		protected virtual void OnDestroy() 
		{
			WidgetInputHandler.UnregisterInput(this.m_KeyCode, this);
		}

		private IEnumerator OnDelayedStart ()
		{
			yield return null;
			if (!IsVisible) {
				gameObject.SetActive (false);
			}
		}

		protected virtual void Update() 
		{
		}
        private static AudioSource audioSource;
        public static void PlaySound (AudioClip clip, float volume)
		{
			if (clip == null) {
				return;
			}
			if (audioSource == null) {
				AudioListener listener = GameObject.FindObjectOfType<AudioListener> ();
				if (listener != null) {
					audioSource = listener.GetComponent<AudioSource> ();
					if (audioSource == null) {
						audioSource = listener.gameObject.AddComponent<AudioSource> ();
					}
				}
			}
			if (audioSource != null) {
				audioSource.PlayOneShot (clip, volume);
			}
		}
		public virtual void Show ()
		{
			
            if (this.m_IsShowing) {
                return;
            }
            this.m_IsShowing = true;
			gameObject.SetActive (true);
           
			TweenCanvasGroupAlpha (m_CanvasGroup.alpha, 1f);
			TweenTransformScale (Vector3.ClampMagnitude (m_RectTransform.localScale, 1.9f), Vector3.one);
			
			PlaySound (this.m_ShowSound, 0.5f);
			m_CanvasGroup.interactable = true;
			m_CanvasGroup.blocksRaycasts = true;
			Canvas.ForceUpdateCanvases();
			for (int i = 0; i < this.m_Scrollbars.Length; i++)
			{
				this.m_Scrollbars[i].value = 1f;
			}
		}
		public virtual void Close ()
		{
            if (!m_IsShowing) {
                return;
            }
            m_IsShowing = false;
			TweenCanvasGroupAlpha (m_CanvasGroup.alpha, 0f);
			TweenTransformScale (m_RectTransform.localScale, Vector3.zero);
			PlaySound (this.m_CloseSound, 0.1f);
			m_CanvasGroup.interactable = false;
			m_CanvasGroup.blocksRaycasts = false;
		}

		private void TweenCanvasGroupAlpha (float startValue, float targetValue)
		{
				FloatTween alphaTween = new FloatTween {
					easeType = m_EaseType,
					duration = m_Duration,
					startValue = startValue,
					targetValue = targetValue,
					ignoreTimeScale = m_IgnoreTimeScale
				};

				alphaTween.AddOnChangedCallback ((float value) => {
					m_CanvasGroup.alpha = value;
				});
				alphaTween.AddOnFinishCallback (() => {
					if (alphaTween.startValue > alphaTween.targetValue) {
						if (!this.m_IsShowing) {
							gameObject.SetActive (false);
						}
					} 
				});
			
			m_AlphaTweenRunner.StartTween (alphaTween);
		}
		private void TweenTransformScale (Vector3 startValue, Vector3 targetValue)
		{
            Vector3Tween scaleTween = new Vector3Tween
            {
                easeType = m_EaseType,
                duration = m_Duration,
                startValue = startValue,
                targetValue = targetValue,
				ignoreTimeScale = m_IgnoreTimeScale
			};
            scaleTween.AddOnChangedCallback((Vector3 value) => {
                m_RectTransform.localScale = value;
            });

            m_ScaleTweenRunner.StartTween(scaleTween);
        }
		public virtual void Toggle ()
		{
			if (!IsVisible) {
				Show ();
			} else {
				Close ();
			}
		}
	}
}
