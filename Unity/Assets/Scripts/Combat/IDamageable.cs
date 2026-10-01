namespace Babel.Runtime.Combat
{
    public interface IDamageable
    {
        TeamAlignment Alignment { get; }
        bool CanReceiveDamage(TeamAlignment sourceTeam);
        void ReceiveDamage(DamageInfo damage);
    }
}
