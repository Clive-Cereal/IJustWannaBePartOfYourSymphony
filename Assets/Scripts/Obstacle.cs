public class Obstacle : Interactable
{
    protected override void OnPlayerContact(Player player)
    {
        player.OnHitObstacle();
    }
}
