using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Grows a few bones of a boss's model to a scale after its clips have posed it (README round 28: the Frost Warden's
    /// icicles, "uniform localScale in LateUpdate"), so its rage shows on its back. View only.
    /// </summary>
    public sealed class BoneScaleView : MonoBehaviour
    {
        private Transform[] _bones = new Transform[0];

        /// <summary>While false the bones are left to the clips.</summary>
        public bool Holding { get; set; } = true;
        private float _scale = 1f;
        private float _shown = 1f;

        // How fast the icicles grow or shrink towards their scale, per second.
        private const float GrowRate = 1.5f;

        public void SetBones(Transform[] bones)
        {
            _bones = bones ?? new Transform[0];
        }

        public void SetScale(float scale, bool instantly = false)
        {
            _scale = scale;
            if (instantly)
            {
                _shown = scale;
            }
        }

        private void LateUpdate()
        {
            if (!Holding)
            {
                return;
            }

            _shown = Mathf.MoveTowards(_shown, _scale, GrowRate * Time.deltaTime);
            for (int i = 0; i < _bones.Length; i++)
            {
                if (_bones[i] != null)
                {
                    _bones[i].localScale = Vector3.one * _shown;
                }
            }
        }
    }
}
