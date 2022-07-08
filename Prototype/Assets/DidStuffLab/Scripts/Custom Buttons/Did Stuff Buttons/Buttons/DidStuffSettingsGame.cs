using DidStuffLab.Scripts.Managers;

namespace DidStuffLab.Scripts.Custom_Buttons.Did_Stuff_Buttons.Buttons
{
    public class DidStuffSettingsGame : OneShotButton
    {
        protected override void ButtonClicked()
        {
            base.ButtonClicked();
            InGameMenuManager.Instance.OpenSettings();
        }
    }
}