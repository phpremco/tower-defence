using System;
using Game.Core;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Drops the editor's cached English tables (<see cref="Loc.ResetFallback"/>) whenever a table file is imported.
    /// Definitions resolve their keys in OnValidate while assets import; without this, a cache filled before a new or
    /// changed table arrived would keep showing keys until the next domain reload.
    /// </summary>
    public sealed class LocalisationImportWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Touches(imported) || Touches(deleted) || Touches(moved))
            {
                Loc.ResetFallback();
            }
        }

        private static bool Touches(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.StartsWith(LocalisationTables.Folder, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
