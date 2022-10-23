using System.Collections.Generic;
using System.Linq;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Colors.Add
{
    public class EditorActionSetColor : IEditorAction
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public EditorActionType Type { get; } = EditorActionType.SetColor;

        /// <summary>
        /// </summary>
        private EditorActionManager ActionManager { get; }

        /// <summary>
        /// </summary>
        public List<int> OriginalHitObjectColors = new List<int>();

        /// <summary>
        /// </summary>
        private List<HitObjectInfo> HitObjects { get; }

        /// <summary>
        /// </summary>
        private int Color { get; }

        /// <summary>
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="hitObjects"></param>
        /// <param name="color"></param>
        public EditorActionSetColor(EditorActionManager manager, List<HitObjectInfo> hitObjects, int color)
        {
            ActionManager = manager;
            HitObjects = hitObjects;
            HitObjects.ForEach(x => OriginalHitObjectColors.Add(x.Color));
            Color = color;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Perform()
        {
            foreach (var ho in HitObjects)
                ho.Color = Color;

            ActionManager.TriggerEvent(EditorActionType.SetColor, new EditorColorSetEventArgs(HitObjects, Color));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Undo() => new EditorActionSetColors(ActionManager, HitObjects, OriginalHitObjectColors).Perform();
    }
}