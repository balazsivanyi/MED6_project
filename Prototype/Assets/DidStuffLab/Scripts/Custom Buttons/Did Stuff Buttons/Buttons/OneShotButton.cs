using UnityEngine;

namespace DidStuffLab.Scripts.Custom_Buttons.Did_Stuff_Buttons.Buttons
{
    public class OneShotButton : AbstractDidStuffButton
    {
      

        protected override void ToggleButton(bool activate)
        {
            ChangeToInactiveState();
            //_currentDwellTime = DwellTime;
            if(Initialised)ActivatedScaleFeedback();
        }

        public void SetInactive() => gameObject.SetActive(false);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Initialised) return;
            _dwellGfx.localScale = dwellScaleX ? new Vector3(0, _originaldwellScaleY, 1) : Vector3.zero;
            ToggleDwellGfx(false);
        }
    }
}
