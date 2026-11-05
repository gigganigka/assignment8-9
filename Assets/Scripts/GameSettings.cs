// Статический класс: живёт всё время работы игры, не привязан к сцене.
// Меню записывает сюда выбор, а скрипты на уровнях читают.
public static class GameSettings
{
    public static int playerMaxHp = 3;          // по заданию HP героя = 3
    public static float fireCooldown = 0.5f;    // пауза между выстрелами, секунды
    public static bool hazardsEnabled = true;   // огонь / шипы
    public static bool enemiesEnabled = true;   // враги
}
