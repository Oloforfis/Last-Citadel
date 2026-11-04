public static class PlayerActionState
{
    // Двигаться нельзя когда true
    public static bool BlockMovement;

    // Применять навыки/атаки нельзя когда true
    public static bool BlockAttack;

    // Множитель скорости движения 
    public static float SpeedMultiplier = 1f;
}
