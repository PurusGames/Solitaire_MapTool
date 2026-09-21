using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public static class TutorialDataHelper
    {
        public static void AutoResolveReferences(TutorialMapTool tool)
        {
            if (tool == null) return;

            if (tool.checkCardObj == null)
            {
                tool.checkCardObj = tool.GetComponentInChildren<TutorialCheckCard>();
                if (tool.checkCardObj == null)
                {
                    tool.checkCardObj = Object.FindObjectOfType<TutorialCheckCard>();
                }
            }

            if (tool.drawPileObj == null)
            {
                tool.drawPileObj = tool.GetComponentInChildren<TutorialDrawPile>();
                if (tool.drawPileObj == null)
                {
                    tool.drawPileObj = Object.FindObjectOfType<TutorialDrawPile>();
                }
            }
        }

        public static void GenerateExtraObjects(TutorialMapTool tool, TutorialLevelData level)
        {
            AutoResolveReferences(tool);

            if (tool.checkCardObj != null)
            {
                if (level.checkCardData != null)
                {
                    tool.checkCardObj.LoadData(level.checkCardData, tool.cardPrefab);
                    if (tool.cardSpriteData != null) 
                    {
                        CardGizmo g = tool.checkCardObj.GetComponentInChildren<CardGizmo>();
                        if (g != null) 
                        { 
                            g.spriteData = tool.cardSpriteData; 
                            g.UpdateVisuals(); 
                        }
                    }
                }
            }

            if (tool.drawPileObj != null)
            {
                if (level.drawPileData != null)
                {
                    tool.drawPileObj.LoadData(level.drawPileData, tool.cardPrefab, tool.positionMultiplier);
                }
                tool.drawPileObj.RefreshVisuals(tool.cardPrefab, tool.positionMultiplier, tool.cardSpriteData);
            }
        }

        public static void SaveExtraObjects(TutorialMapTool tool, TutorialLevelData level)
        {
            AutoResolveReferences(tool);

            if (tool.checkCardObj != null)
            {
                level.checkCardData = tool.checkCardObj.SaveData();
            }

            if (tool.drawPileObj != null)
            {
                level.drawPileData = tool.drawPileObj.SaveData();
            }
        }
    }
}
