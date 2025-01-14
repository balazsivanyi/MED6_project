using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DidStuffLab.Scripts.Custom_Buttons.Did_Stuff_Buttons.Buttons.One_Option
{
    public class OneOption : AbstractDidStuffButton
    {
        
        
        protected List<OneOption> OtherButtonsToDisable = new  List<OneOption>();

        [SerializeField] private AbstractDidStuffButton buttonToEnableOnChoice;
        [SerializeField] private bool initialised = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!initialised) return;
        }

        protected override void Start()
        {
            base.Start();
            var otherButtons = transform.parent.GetComponentsInChildren<OneOption>().Where(btn => btn != this).ToList();
            OtherButtonsToDisable = otherButtons;
        }
        

        protected override void ButtonClicked()
        {
            base.ButtonClicked();
            SetCanHover(false);
            
            foreach (var btn in OtherButtonsToDisable)
            {
                btn.DeactivateButton();
                btn.SetCanHover(true);
            }
            
        }
        
        
        protected override void StartInteractionCoolDown() { }

        protected override void ChangeToActiveState()
        {
            if(buttonToEnableOnChoice != null)
                if(buttonToEnableOnChoice.IsDisabled) buttonToEnableOnChoice.EnableButton();
            base.ChangeToActiveState();
            SetCanHover(false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DeactivateButton();
            SetCanHover(true);
            //buttonToEnableOnChoice.DisableButton();
            initialised = true;
        }
    }
}
