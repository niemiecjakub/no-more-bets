namespace NoMoreBets.Infrastructure.AI.Common;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AgentToolAttribute : Attribute
{
  public AgentToolAttribute()
  {
  }

  public AgentToolAttribute(string name)
  {
    Name = name;
  }

  public string? Name { get; }
}
