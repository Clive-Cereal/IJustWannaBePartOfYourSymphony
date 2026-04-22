public class Coins : Interactable
{
    protected override void OnPlayerContact(Player player)
    {
        player.OnHitCoins();
    }
}
