namespace KingOfTokyo.Core.Abstractions;

public abstract class GameRuleResolverBase : IRuleResolver
{
    public abstract string Name { get; }
}