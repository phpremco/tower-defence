namespace Game.PathObjects
{
    /// <summary>Which procedural model a path object wears. None falls back to a plain shape chosen by its kind.</summary>
    public enum PathObjectLook
    {
        None = 0,
        Mine = 1,
        FieldEmitter = 2,
        LavaBall = 3,
        GroundDrone = 4,
        Barrel = 5,

        /// <summary>The Chrono Field: an emitter with counter-rotating rings and motes that drift up in slow motion.</summary>
        Chrono = 7,
    }
}
