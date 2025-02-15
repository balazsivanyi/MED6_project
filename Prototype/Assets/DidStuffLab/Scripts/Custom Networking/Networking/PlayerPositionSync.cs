using Mirror;

namespace DidStuffLab {
    public class PlayerPositionSync : CustomSyncBehaviour<bool> {
        public bool Position1Occupied => Value.Value;

        protected override void Initialize() { }

        [Command(requiresAuthority = false)]
        protected override void CmdUpdateValue(bool newValue) => Value.Value = newValue;

        public void SetCorrespondingPosition() {
            // if Position1Occupied is still false on the server, then it means that there is currently no players occupying that space
            // so I'm the first player --> so occupy that position
            print("The server says that position 1 is occupied --> " + Value.Value);
            if(!Value.Value) SendToServer(true);
        }

        protected override void UpdateValueLocally(bool newValue) {
            base.UpdateValueLocally(newValue);
            print("Position1Occupied has changed from the server to: " + newValue);
        }
    }
}