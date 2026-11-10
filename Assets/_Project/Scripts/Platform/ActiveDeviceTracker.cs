namespace Game.Platform
{
    /// <summary>
    /// Remembers which kind of device the player used last and which gamepad that was, so prompts follow the hands and
    /// only the pad in use pauses the game when it drops out. Pure: the provider feeds it device ids.
    /// </summary>
    public sealed class ActiveDeviceTracker
    {
        public const int NoDevice = -1;

        public ActiveDeviceTracker(InputDeviceKind initial = InputDeviceKind.KeyboardMouse)
        {
            Kind = initial;
        }

        public InputDeviceKind Kind { get; private set; }

        /// <summary>The id of the gamepad used last, or <see cref="NoDevice"/> before any pad was used.</summary>
        public int GamepadId { get; private set; } = NoDevice;

        /// <summary>True while the pad in use is disconnected; cleared when a pad is connected or used again.</summary>
        public bool GamepadLost { get; private set; }

        /// <summary>Records a use of a device; returns true when the active kind changed.</summary>
        public bool Used(InputDeviceKind kind, int deviceId)
        {
            if (kind == InputDeviceKind.Gamepad)
            {
                GamepadId = deviceId;
                GamepadLost = false;
            }

            if (kind == Kind)
            {
                return false;
            }

            Kind = kind;
            return true;
        }

        /// <summary>A device went away. True when it was the gamepad the player was using: that is the moment to pause.</summary>
        public bool Lost(int deviceId)
        {
            if (deviceId == NoDevice || deviceId != GamepadId || Kind != InputDeviceKind.Gamepad || GamepadLost)
            {
                return false;
            }

            GamepadLost = true;
            return true;
        }

        /// <summary>
        /// A gamepad came (back). True while the pad in use is lost: a reconnected pad may come back under a new id, so any
        /// pad ends the loss and becomes the pad in use.
        /// </summary>
        public bool Regained(int gamepadId)
        {
            if (!GamepadLost)
            {
                return false;
            }

            GamepadLost = false;
            GamepadId = gamepadId;
            return true;
        }
    }
}
