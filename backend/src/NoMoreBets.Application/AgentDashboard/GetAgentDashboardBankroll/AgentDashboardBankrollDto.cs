namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBankroll;

public record AgentDashboardBankrollDto(decimal TotalValue, decimal Balance, int DaysUntilPayday);
