namespace BilliotGames
{
    public interface IPushCondition
    {
        bool CanPush(ItemStack item); 
        int GetAllowedAmount(ItemStack item);
    }
}