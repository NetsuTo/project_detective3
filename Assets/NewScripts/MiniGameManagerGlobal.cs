using UnityEngine;
using System.Collections.Generic;

public class MiniGameManagerGlobal : MonoBehaviour
{
    // ? เก็บรายการมินิเกมทั้งหมดในฉาก
    private static List<ElementMiniGameManager> allMiniGames = new List<ElementMiniGameManager>();

    // ? มินิเกมที่ active อยู่ตอนนี้
    public static ElementMiniGameManager activeMiniGame = null;

    public static void RegisterMiniGame(ElementMiniGameManager game)
    {
        if (!allMiniGames.Contains(game))
            allMiniGames.Add(game);
    }

    public static void UnregisterMiniGame(ElementMiniGameManager game)
    {
        if (allMiniGames.Contains(game))
            allMiniGames.Remove(game);
    }

    public static void Activate(ElementMiniGameManager target)
    {
        // ปิดตัวเก่าก่อน
        if (activeMiniGame != null && activeMiniGame != target)
        {
            activeMiniGame.ForceStop(); // ปิดเฉพาะ display / input แต่ไม่แตะ event
        }

        activeMiniGame = target;
    }

    public static bool IsActive(ElementMiniGameManager target)
    {
        return activeMiniGame == target;
    }

    public static void Clear()
    {
        activeMiniGame = null;
    }
}
