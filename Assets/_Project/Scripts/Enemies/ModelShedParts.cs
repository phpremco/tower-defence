using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The parts of an imported creature that come off as its armour is chipped away (a Shardback's prism clusters):
    /// bones named by its art, in shedding order, scaled to zero once shed. Every clip keys those bones at full size, so
    /// the owner re-applies the hidden ones each LateUpdate, after the animation has written the pose. Pure view code.
    /// </summary>
    public sealed class ModelShedParts
    {
        private readonly Transform[][] _bones;
        private readonly Transform _fallback;

        public ModelShedParts(CreatureModel model)
        {
            string[] names = model.Art.ShedBones;
            _bones = new Transform[names.Length][];
            for (int i = 0; i < names.Length; i++)
            {
                _bones[i] = model.Bones(names[i]);
            }

            _fallback = model.Root;
        }

        /// <summary>How many parts the model sheds in all.</summary>
        public int Count => _bones.Length;

        /// <summary>How many parts have come off, the first ones in shedding order first.</summary>
        public int Shed { get; private set; }

        /// <summary>Where a part sits right now (its bone in the near rig), for the debris it throws.</summary>
        public Vector3 Position(int index)
        {
            Transform[] bones = _bones[index];
            return bones.Length > 0 ? bones[0].position : _fallback.position;
        }

        /// <summary>Hides the first parts in shedding order from the next <see cref="Apply"/> on (zero puts them all back).</summary>
        public void SetShed(int count) => Shed = Mathf.Clamp(count, 0, _bones.Length);

        /// <summary>Scales the shed parts to zero over the pose the clips wrote; parts still on the body are left alone.</summary>
        public void Apply()
        {
            for (int i = 0; i < Shed; i++)
            {
                Transform[] bones = _bones[i];
                for (int b = 0; b < bones.Length; b++)
                {
                    bones[b].localScale = Vector3.zero;
                }
            }
        }
    }
}
