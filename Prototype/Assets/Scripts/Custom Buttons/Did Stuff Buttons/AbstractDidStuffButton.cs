using System.Collections;
using System.Linq;
using TMPro;
using Tobii.Gaming;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static UnityEngine.Vector3;
using Image = UnityEngine.UI.Image;

namespace Custom_Buttons.Did_Stuff_Buttons
{
#if UNITY_EDITOR

	[CustomEditor(typeof(AbstractDidStuffButton), true)]
	public class DidStuffButtonEditor : Editor
	{
		private Sprite SpriteField(string name, Sprite sprite)
		{
			GUILayout.BeginVertical();
			var style = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.UpperCenter,
				fixedWidth = 70
			};
			GUILayout.Label(name, style);
			var result = (Sprite) EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Width(70),
				GUILayout.Height(70));
			GUILayout.EndVertical();
			return result;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var abstractDidStuffButton = (AbstractDidStuffButton) target;
			abstractDidStuffButton.customHoverColours = GUILayout.Toggle(abstractDidStuffButton.customHoverColours,
				"Use Custom Hover Colours");
			abstractDidStuffButton.useInteractableLayer = GUILayout.Toggle(abstractDidStuffButton.useInteractableLayer,
				"Use Interactable Layer");
			abstractDidStuffButton.changeTextOrIconColour = GUILayout.Toggle(
				abstractDidStuffButton.changeTextOrIconColour, "Change the color of the text or icon on activation");
			abstractDidStuffButton.useIcon =
				GUILayout.Toggle(abstractDidStuffButton.useIcon, "Use an icon on the button");
			abstractDidStuffButton.useText = GUILayout.Toggle(abstractDidStuffButton.useText, "Use text on the button");
			abstractDidStuffButton.useSecondaryText = GUILayout.Toggle(abstractDidStuffButton.useSecondaryText, "Use secondary text on the button");
			abstractDidStuffButton.interactionSetting = GUILayout.Toggle(abstractDidStuffButton.interactionSetting, "Does this button set the interaction method");
			abstractDidStuffButton.dwellTimeSetting = GUILayout.Toggle(abstractDidStuffButton.dwellTimeSetting, "Does this button set dwell time");

			if (abstractDidStuffButton.useInteractableLayer)
			{
				abstractDidStuffButton.interactableLayer = EditorGUILayout.LayerField("Interactable Layer",
					abstractDidStuffButton.interactableLayer);
			}

			if (abstractDidStuffButton.customHoverColours)
			{
				abstractDidStuffButton.activeHoverColour = EditorGUILayout.ColorField("Active Hover Colour",
					abstractDidStuffButton.activeHoverColour);
				abstractDidStuffButton.inactiveHoverColour = EditorGUILayout.ColorField("Inactive Hover Colour",
					abstractDidStuffButton.inactiveHoverColour);
			}

			if (abstractDidStuffButton.changeTextOrIconColour)
			{
				abstractDidStuffButton.activeTextOrIconColour = EditorGUILayout.ColorField("Active Text or Icon Colour",
					abstractDidStuffButton.activeTextOrIconColour);
				abstractDidStuffButton.inactiveTextOrIconColour =
					EditorGUILayout.ColorField("Inactive Text or Icon Colour",
						abstractDidStuffButton.inactiveTextOrIconColour);
			}

			if (abstractDidStuffButton.useIcon)
				abstractDidStuffButton.iconImg = SpriteField("Icon Image", abstractDidStuffButton.iconImg);
			//else button.useIcon = EditorGUILayout.TextField("Button Text", button.text);
			if (abstractDidStuffButton.useText)
				abstractDidStuffButton.primaryText = EditorGUILayout.TextField("Button Text", abstractDidStuffButton.primaryText);
			if (abstractDidStuffButton.useSecondaryText)
				abstractDidStuffButton.secondaryText = EditorGUILayout.TextField("Button secondary Text", abstractDidStuffButton.secondaryText,  GUILayout.Height(100));
			if (abstractDidStuffButton.interactionSetting)
				abstractDidStuffButton.localInteractionMethod = (InteractionMethod)EditorGUILayout.EnumPopup("Local interaction method",
					abstractDidStuffButton.localInteractionMethod);
			if(abstractDidStuffButton.dwellTimeSetting)
				abstractDidStuffButton.localDwellTime = EditorGUILayout.FloatField("Local dwell time to set",
				abstractDidStuffButton.localDwellTime);






		}
	}

#endif

	public enum InteractionMethod
	{
		Mouse,
		MouseDwell,
		Tobii,
		Touch
	}
	
	public abstract class AbstractDidStuffButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		#region Fields

		private delegate void Clicked();

		private event Clicked OnClick;

		private delegate void Hovered();

		private event Hovered OnHover;

		private delegate void UnHovered();

		private event UnHovered OnUnHover;

		private delegate void Deactivated();

		private event Deactivated OnDeactivate;

		private delegate void Activated();

		private event Activated OnActivate;
		
		private delegate void RunInteractionMethod();
		private RunInteractionMethod RunInteraction;
		

		[SerializeField] private UnityEvent onClicked;
		
		[SerializeField]
		protected Color activeColour = Color.green, inactiveColour = Color.red, disabledColour = Color.grey;
		private RectTransform _dwellGfx;
		
		[HideInInspector] public string primaryText, secondaryText;
		[HideInInspector] public bool useInteractableLayer;
		[HideInInspector] public LayerMask interactableLayer;
		[HideInInspector] public bool customHoverColours = false;
		

		[HideInInspector, Header("Custom Hover Colours")]
		public Color inactiveHoverColour, activeHoverColour;

		[HideInInspector] public bool changeTextOrIconColour = false;
		[HideInInspector] public Color activeTextOrIconColour = Color.white, inactiveTextOrIconColour = Color.black;
		[HideInInspector] public bool useIcon, useText;
		[HideInInspector] public bool useSecondaryText = false;
		[HideInInspector] public Sprite iconImg;
		[HideInInspector] public bool interactionSetting;
		[HideInInspector] public bool dwellTimeSetting;
		[HideInInspector] public InteractionMethod localInteractionMethod;
		[HideInInspector] public float localDwellTime = 1.0f;

		private bool _mouseHover = false, _canHover = true;
		protected bool _isActive = true, _isHover, _isDisabled;
		private Image _mainImage;
		private Image _iconImage;
		private Image _dwellGfxImg;
		private bool _provideDwellFeedbackLocal = false;
		private static bool _provideDwellFeedbackGlobal;
		private TextMeshProUGUI _primaryText, _secondaryText;
		private Animator _dwellAnimator;
		private float _interactionBreakTime = 1.0f;
		private GazeAware _gazeAware;
		protected Camera MainCamera;
		private static float _dwellTime = 1.0f;
		private static InteractionMethod _interactionMethod = InteractionMethod.Mouse;
		private float _currentDwellTime = 0.0f;
		private bool _initialised;
		private bool _playActivatedScale;
		private Collider _collider;

		
		#endregion

		protected float DwellTime
		{
			get => _dwellTime;
			set => _dwellTime = value;
		}

		protected void SetInteractionMethod(InteractionMethod method)
		{
			_interactionMethod = method;
			if (_interactionMethod ==InteractionMethod.Tobii ||
			     _interactionMethod == InteractionMethod.MouseDwell)
			{
				_provideDwellFeedbackGlobal = true;
			}
			else _provideDwellFeedbackGlobal = false;
		}

		protected InteractionMethod GetInteractionMethod() => _interactionMethod;
		
		protected virtual void OnEnable()
		{
			OnClick += ButtonClicked;
			OnHover += ButtonHovered;
			OnUnHover += ButtonUnHovered;
			OnActivate += ActivateButton;
			OnDeactivate += DeactivateButton;
			
			if (DelegateInteractionMethod(true)) return;
			
			DeactivateButton();
		}

		private bool DelegateInteractionMethod(bool enable) {
			if (!interactionSetting) {
				switch (_interactionMethod) {
					case InteractionMethod.MouseDwell:
						if(enable) RunInteraction += DwellScale;
						else RunInteraction -= DwellScale;
						break;
					case InteractionMethod.Mouse:
						if(enable) RunInteraction += MouseInput;
						else RunInteraction -= MouseInput;
						break;
					case InteractionMethod.Tobii:
						if (!TobiiAPI.IsConnected) return true;
						if(enable) {
							RunInteraction += TobiiInput;
							RunInteraction += DwellScale;
						}
						else {
							RunInteraction -= TobiiInput;
							RunInteraction -= DwellScale;
						}
						break;
					case InteractionMethod.Touch:
						if(enable) RunInteraction += TouchInput;
						else RunInteraction -= TouchInput;
						break;
				}
			}
			else {
				switch (localInteractionMethod) {
					case InteractionMethod.MouseDwell:
						if (enable) RunInteraction += DwellScale;
						else RunInteraction -= DwellScale;
						break;
					case InteractionMethod.Mouse:
						RunInteraction += MouseInput;
						if (enable) RunInteraction += MouseInput;
						else RunInteraction -= MouseInput;
						break;
					case InteractionMethod.Tobii:
						if (!TobiiAPI.IsConnected) return true;
						if(enable) {
							RunInteraction += TobiiInput;
							RunInteraction += DwellScale;
						}
						else {
							RunInteraction -= TobiiInput;
							RunInteraction -= DwellScale;
						}
						break;
					case InteractionMethod.Touch:
						if(enable) RunInteraction += TouchInput;
						else RunInteraction -= TouchInput;
						break;
				}
			}

			return false;
		}

		protected void SetNewDwellTime()
		{
			PlayerPrefs.SetFloat("DwellTime", _dwellTime);
		}
		
		protected virtual void Awake()
		{
			_mainImage = GetComponent<Image>();
			_dwellAnimator = GetComponentInChildren<Animator>();
			_gazeAware = GetComponent<GazeAware>();
			MainCamera = Camera.main;
			if (!useInteractableLayer) interactableLayer = ~0;
			GetTheChildren();
			if (!customHoverColours) SetAutomaticColours();
			else SetColours();

			if (_interactionMethod == InteractionMethod.Tobii ||
			    _interactionMethod == InteractionMethod.MouseDwell) _provideDwellFeedbackGlobal = true;
			else _provideDwellFeedbackGlobal = false;

			if (interactionSetting && (localInteractionMethod == InteractionMethod.Tobii ||
			                           localInteractionMethod == InteractionMethod.MouseDwell))
				_provideDwellFeedbackLocal = true;
			else _provideDwellFeedbackLocal = false;

			if (PlayerPrefs.GetFloat("DwellTime") != 0.0f)
				DwellTime = PlayerPrefs.GetFloat("DwellTime");
			else DwellTime = 1.0f;

			_collider = GetComponent<Collider>();
			
			var rt = GetComponent<RectTransform>().rect;
			var w = rt.width;
			var h = rt.height;
			
			_dwellGfx.sizeDelta = new Vector2(w, h);
			_interactionMethod = (InteractionMethod)PlayerPrefs.GetInt("InteractionMethod");
			
			if (_interactionMethod != InteractionMethod.Tobii) ActivateCollider(false);
			
			ToggleDwellGfx(false);
			DeactivateButton();
		}
		
		private void GetTheChildren()
		{
			
			if(GetComponentsInChildren<Image>().Where(r => r.CompareTag("ButtonIcon")).ToArray()[0] != null)
				_iconImage = GetComponentsInChildren<Image>().Where(r => r.CompareTag("ButtonIcon")).ToArray()[0];
			if(GetComponentsInChildren<RectTransform>().Where(r => r.CompareTag("DwellGfx")).ToArray()[0] != null)
				_dwellGfx = GetComponentsInChildren<RectTransform>().Where(r => r.CompareTag("DwellGfx")).ToArray()[0];
			if (GetComponentsInChildren<TextMeshProUGUI>().Where(r => r.CompareTag("ButtonPrimaryText")).ToArray()[0] !=
			    null)
				_primaryText = GetComponentsInChildren<TextMeshProUGUI>().Where(r => r.CompareTag("ButtonPrimaryText"))
					.ToArray()[0];
			if (GetComponentsInChildren<TextMeshProUGUI>().Where(r => r.CompareTag("ButtonSecondaryText")).ToArray()[0] !=
			    null)
				_secondaryText = GetComponentsInChildren<TextMeshProUGUI>().Where(r => r.CompareTag("ButtonSecondaryText"))
					.ToArray()[0];
			
			_dwellGfxImg = _dwellGfx.GetComponent<Image>();
			
			if (useIcon) _iconImage.sprite = iconImg;
			else _iconImage.gameObject.SetActive(false);
			if (useText) _primaryText.text = primaryText;
			else _primaryText.transform.gameObject.SetActive(false);
			if (useSecondaryText) _secondaryText.text = secondaryText;
			else _secondaryText.transform.gameObject.SetActive(false);
		}
		
		protected virtual void Update() {
			RunInteraction?.Invoke();

			if (!_playActivatedScale) return;
			if (_dwellGfx.localScale.x > 0.0f) _dwellGfx.localScale -= one * 0.01f;
			else ToggleDwellGfx(false);

		}

		protected virtual void ButtonClicked()
		{
			ToggleButton(!_isActive);
			onClicked?.Invoke();
			StartInteractionCoolDown();
		}

		protected void ActivatedScaleFeedback()
		{
			// ToggleDwellGfx(true);
			_dwellGfx.localScale = one;
			_playActivatedScale = true;
		}

		private void ToggleDwellGfx(bool activate) {
			var color = _dwellGfxImg.color;
			if (!activate) _playActivatedScale = false;
			_dwellGfxImg.color = new Color(color.r, color.b, color.g,  activate ? 255 : 0);
			// _dwellGfx.transform.gameObject.SetActive(activate);
		}

		protected void ActivateCollider(bool activate) => _collider.enabled = activate;

		protected void ActivateText(bool activate) => _primaryText.gameObject.SetActive(activate);
		
		protected void InvokeOnClickUnityEvent() => onClicked?.Invoke();

		protected virtual void StartInteractionCoolDown()
		{
			if (_interactionMethod == InteractionMethod.Tobii || _interactionMethod == InteractionMethod.MouseDwell)
				StartCoroutine(CoolDownTime());
		}

		private void ButtonHovered()
		{
			if (!_canHover) return;
			if (!_initialised) _initialised = true;
			if (!interactionSetting)
			{
				switch (_interactionMethod)
				{
					case InteractionMethod.MouseDwell:
						_currentDwellTime = dwellTimeSetting ? localDwellTime : _dwellTime;
						ToggleDwellGfx(true);
						break;
					case InteractionMethod.Mouse:
						MouseHover();
						break;
					case InteractionMethod.Tobii:
						if (!TobiiAPI.IsConnected) return;
						_currentDwellTime = dwellTimeSetting ? localDwellTime : _dwellTime;
						ToggleDwellGfx(true);
						break;
					case InteractionMethod.Touch:
						break;
				}
			}
			else
				
			{
				switch (localInteractionMethod)
				{
					case InteractionMethod.MouseDwell:
						_currentDwellTime = dwellTimeSetting ? localDwellTime : _dwellTime;
						ToggleDwellGfx(true);
						break;
					case InteractionMethod.Mouse:
						MouseHover();
						break;
					case InteractionMethod.Tobii:
						if (!TobiiAPI.IsConnected) return;
						_currentDwellTime = dwellTimeSetting ? localDwellTime : _dwellTime;
						ToggleDwellGfx(true);
						break;
					case InteractionMethod.Touch:
						break;
				}

			}
		}

		private void ButtonUnHovered()
		{
			if(!interactionSetting){switch (_interactionMethod)
			{
				case InteractionMethod.MouseDwell:
					
					break;
				case InteractionMethod.Mouse:
					MouseUnHover();
					break;
				case InteractionMethod.Tobii:
					
					break;
				case InteractionMethod.Touch:
					break;
			}}
			else
			{
				switch (localInteractionMethod)
				{
					case InteractionMethod.MouseDwell:
						
						break;
					case InteractionMethod.Mouse:
						MouseUnHover();
						break;
					case InteractionMethod.Tobii:
						
						break;
					case InteractionMethod.Touch:
						break;
				}
			}
		}

		protected void SetCanHover(bool canHover)
		{
			_canHover = canHover;
		} 
		//Call this if you want to change the state of the button with no events being called. Like if you want to activate a DuoRhythmo drum node from the server.
		public virtual void ActivateButton() => ToggleButton(true);

		protected void ActivateAndCallEvents() => OnClick?.Invoke();

		public virtual void DeactivateButton() => ToggleButton(false);

		protected virtual void ToggleButton(bool activate)
		{
			if (activate) ChangeToActiveState();
			else ChangeToInactiveState();
		}

		protected virtual void ChangeToActiveState()
		{
			_isActive = true; 
			_mainImage.color = activeColour;
			_dwellGfxImg.color = inactiveColour;
			if (useIcon && changeTextOrIconColour) _iconImage.color = activeTextOrIconColour;
			if (useText && changeTextOrIconColour) _primaryText.color = activeTextOrIconColour;
			if (useSecondaryText && changeTextOrIconColour) _secondaryText.color = activeTextOrIconColour;
		}

		protected virtual void ChangeToInactiveState()
		{
			_isActive = false;
			_mainImage.color = inactiveColour;
			_dwellGfxImg.color = activeColour;
			if (useIcon && changeTextOrIconColour) _iconImage.color = inactiveTextOrIconColour;
			if (useText && changeTextOrIconColour) _primaryText.color = inactiveTextOrIconColour;
			if (useSecondaryText && changeTextOrIconColour) _secondaryText.color = inactiveTextOrIconColour;
		}

		private void SetColours()
		{
			_mainImage.color = inactiveColour;
			_dwellGfxImg.color = activeColour;
			if (useText) _primaryText.color = _secondaryText.color = inactiveColour;
			if (useIcon) _iconImage.color = inactiveColour;
		}

		public void SetActiveColoursExplicit(Color newActiveColor, Color newInactiveColor)
		{
			activeColour = newActiveColor;
			inactiveColour = newInactiveColor;
			SetAutomaticColours();
		}

		protected void SetTemporaryColor(Color col)
		{
			_mainImage.color = col;
		}

		public void SetText(string t)
		{
			_primaryText.text = t;
		}

		private void SetAutomaticColours()
		{
			Color.RGBToHSV(inactiveColour, out var uH, out var uS, out var uV);
			uV -= 0.3f;
			Color.RGBToHSV(activeColour, out var aH, out var aS, out var aV);
			aV -= 0.3f;

			inactiveHoverColour = Color.HSVToRGB(uH, uS, uV);
			activeHoverColour = Color.HSVToRGB(aH, aS, aV);

			inactiveHoverColour.a = 1;
			activeHoverColour.a = 1;
			_mainImage.color = inactiveColour;
			_dwellGfxImg.color = activeColour;
		}
		

		#region MouseInteraction

		private void MouseInput()
		{
			if (_isHover && Input.GetMouseButtonDown(0)) OnClick?.Invoke();
		}

		private void MouseHover()
		{
			_mainImage.color = _isActive ? activeHoverColour : inactiveHoverColour;
		}

		private void MouseUnHover()
		{
			_mainImage.color = _isActive ? activeColour : inactiveColour;
		}

		#endregion
		
		#region Dwell
		
		protected virtual void DwellScale()
		{
			if ((!_provideDwellFeedbackGlobal && !interactionSetting) || !_canHover) return;
			var d = dwellTimeSetting ? localDwellTime : _dwellTime;
			if(!_dwellGfx.gameObject.activeInHierarchy) ToggleDwellGfx(true);
			if (_isHover && _currentDwellTime > 0) _currentDwellTime -= Time.deltaTime;
			else if(_isHover &&_currentDwellTime <= 0) DwellActivated();
			else if (!_isHover && _currentDwellTime < d) _currentDwellTime += Time.deltaTime;
			/*if(_isHover||(_currentDwellTime < 1 && _currentDwellTime > 0))
				_dwellGfx.localScale = one - new Vector3(_currentDwellTime, _currentDwellTime, _currentDwellTime);*/
			if (_isHover || (_currentDwellTime < 1 && _currentDwellTime > 0))
			{
				var size = 0.0f;
				if(!dwellTimeSetting) size = Map(_currentDwellTime, _dwellTime, 0, 1f, 0f);
				else size = Map(localDwellTime, _dwellTime, 0, 1f, 0f);
				_dwellGfx.localScale = one - new Vector3(size,size,size);
			}
			

			if(_currentDwellTime > d) ToggleDwellGfx(false);
		}

		private float Map(float value, float min1, float max1, float min2, float max2) {
			return min2 + (max2 - min2) * ((value - min1) / (max1 - min1));
		}

		
		private void DwellActivated()
		{
			StartInteractionCoolDown();
			_currentDwellTime = _dwellTime;
			_dwellGfx.localScale = zero;
			if (!_isActive) _dwellGfxImg.color = inactiveColour;
			else _dwellGfxImg.color = activeColour;
			ToggleDwellGfx(false);
			OnClick?.Invoke();
		}
			
		#endregion
		
		#region TobiiInteraction

		private void TobiiInput()
		{
			if (!TobiiAPI.IsConnected) return;
			if (_gazeAware.HasGazeFocus)
			{
				_isHover = true;
				OnHover?.Invoke();
			}
			else
			{
				_isHover = false;
				OnUnHover?.Invoke();
			}
		}

		#endregion

		#region TouchInteraction

		private void TouchInput()
		{
			if (Input.touchCount <= 0) return;
			var touch = Input.GetTouch(0);
			if (!Physics.Raycast(MainCamera.ScreenToWorldPoint(touch.position), forward, out var hit,
				interactableLayer)) return;
			if (hit.transform == this.transform)
			{
				OnClick?.Invoke();
			}
		}

		#endregion

		#region MouseOverEvents

		private void OnMouseOver()
		{
			if(!_canHover) return;
			_isHover = true;
			OnHover?.Invoke();
		}

		private void OnMouseExit()
		{
			_isHover = false;
			OnUnHover?.Invoke();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if(!_canHover || localInteractionMethod == InteractionMethod.Tobii) return;
			_isHover = true;
			OnHover?.Invoke();
		}
  
		public void OnPointerExit(PointerEventData eventData)
		{
			_isHover = false;
			OnUnHover?.Invoke();
		}
		#endregion
		
		private IEnumerator CoolDownTime()
		{
			_canHover = false;
			yield return new WaitForSeconds(_interactionBreakTime);
			_canHover = true;

		}
		protected virtual void OnDisable()
		{
			OnClick -= ButtonClicked;
			OnHover -= ButtonHovered;
			OnUnHover -= ButtonUnHovered;
			OnActivate -= ActivateButton;
			OnDeactivate -= DeactivateButton;

			DelegateInteractionMethod(false);
		}
	}
}