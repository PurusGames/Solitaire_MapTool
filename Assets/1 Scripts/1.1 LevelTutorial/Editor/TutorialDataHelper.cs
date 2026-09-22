using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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

            SaveTutorialConfig(tool, level);
        }

        public static void SaveTutorialConfig(TutorialMapTool tool, TutorialLevelData level)
        {
            if (tool == null || level == null) return;

            bool hasSteps = tool.tutorialSteps != null && tool.tutorialSteps.Count > 0;
            bool hasFlags = tool.tapDrawPile || tool.tapUndo || tool.tapJoker;
            bool isTutorial = level.type == "tutorial";

            if (isTutorial && (hasSteps || hasFlags))
            {
                if (level.tutorialConfig == null)
                {
                    level.tutorialConfig = new TutorialConfig();
                }

                level.tutorialConfig.tap_card = new List<int>();
                if (hasSteps)
                {
                    foreach (var card in tool.tutorialSteps)
                    {
                        if (card != null)
                        {
                            level.tutorialConfig.tap_card.Add(card.cardId);
                        }
                    }
                }

                level.tutorialConfig.tap_drawpile = tool.tapDrawPile;
                level.tutorialConfig.tap_undo = tool.tapUndo;
                level.tutorialConfig.tap_joker = tool.tapJoker;
            }
            else
            {
                level.tutorialConfig = null;
            }
        }

        public static void LoadTutorialConfig(TutorialMapTool tool, TutorialLevelData level, Dictionary<int, CardGizmo> idToGizmo)
        {
            if (tool == null) return;

            tool.tutorialSteps.Clear();
            tool.tapDrawPile = false;
            tool.tapUndo = false;
            tool.tapJoker = false;

            if (level != null && level.tutorialConfig != null)
            {
                tool.tapDrawPile = level.tutorialConfig.tap_drawpile;
                tool.tapUndo = level.tutorialConfig.tap_undo;
                tool.tapJoker = level.tutorialConfig.tap_joker;

                if (level.tutorialConfig.tap_card != null)
                {
                    foreach (int cid in level.tutorialConfig.tap_card)
                    {
                        if (idToGizmo != null && idToGizmo.TryGetValue(cid, out CardGizmo targetGizmo))
                        {
                            tool.tutorialSteps.Add(targetGizmo);
                        }
                        else
                        {
                            Debug.LogWarning($"[TutorialConfig] Card ID {cid} in tap_card not found in generated scene cards.");
                        }
                    }
                }
            }

            tool.SyncTutorialSteps();
        }
    }
}
