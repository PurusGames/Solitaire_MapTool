using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public static class TutorialDataHelper
    {
        public static void GenerateExtraObjects(TutorialMapTool tool, TutorialLevelData level)
        {
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
                    tool.drawPileObj.LoadData(level.drawPileData, tool.cardPrefab, tool.positionMultiplier);
                
                // Initialize SpriteData for children
                if (tool.cardSpriteData != null)
                {
                    CardGizmo[] pileGizmos = tool.drawPileObj.GetComponentsInChildren<CardGizmo>();
                    foreach (var g in pileGizmos)
                    {
                        g.spriteData = tool.cardSpriteData;
                        g.UpdateVisuals();
                    }
                }
            }
        }

        public static void SaveExtraObjects(TutorialMapTool tool, TutorialLevelData level)
        {
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
