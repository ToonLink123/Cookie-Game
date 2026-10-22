using UnityEngine;

public enum GameState
{
    MainMenu,
    Gameplay,
    Paused,
    Victory,
    GameOver
}

public class GameplayManager : MonoBehaviour
{
    public static GameState currentState = GameState.MainMenu;
}
