using System.Linq.Expressions;
using Hangfire;

namespace NoMoreBets.Infrastructure
{
  internal sealed class RecurringJobRegistrationBuilder(BackgroundJobs.RecurringJobRegistry registry)
  {
    public JobBuilder<T> For<T>(Expression<Func<T, Task>> methodCall) => new(registry, methodCall);
  }

  internal sealed class JobBuilder<T>(
    BackgroundJobs.RecurringJobRegistry registry,
    Expression<Func<T, Task>> methodCall)
  {
    private string _id = string.Empty;
    private string _group = string.Empty;
    private int _order = int.MaxValue;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _cron = string.Empty;
    private bool _isVisible;

    public JobBuilder<T> WithId(string id)
    {
      _id = id;
      return this;
    }

    public JobBuilder<T> WithGroup(string group)
    {
      _group = group;
      return this;
    }

    public JobBuilder<T> WithOrder(int order)
    {
      _order = order;
      return this;
    }

    public JobBuilder<T> WithName(string name)
    {
      _name = name;
      return this;
    }

    public JobBuilder<T> WithDescription(string description)
    {
      _description = description;
      return this;
    }

    public JobBuilder<T> Visible(bool isVisible = true)
    {
      _isVisible = isVisible;
      return this;
    }

    public JobBuilder<T> WithCron(string cron)
    {
      _cron = cron;
      return this;
    }

    public void Register()
    {
      if (string.IsNullOrWhiteSpace(_id) || string.IsNullOrWhiteSpace(_group) || string.IsNullOrWhiteSpace(_name) || string.IsNullOrWhiteSpace(_description) || string.IsNullOrWhiteSpace(_cron))
      {
        throw new InvalidOperationException("Recurring job has incomplete metadata.");
      }

      RecurringJob.AddOrUpdate(_id, methodCall, _cron);
      registry.Register(new BackgroundJobs.JobMetadata(_id, _group, _order, _name, _description, _cron, _isVisible));
    }
  }
}

namespace NoMoreBets.Infrastructure.BackgroundJobs
{
  public sealed record JobMetadata(
    string Id,
    string Group,
  int Order,
    string Name,
    string Description,
    string CronExpression,
    bool IsVisible);
}
