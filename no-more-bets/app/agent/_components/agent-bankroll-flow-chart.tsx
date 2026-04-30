import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { BankrollFlowPointDto } from "@/features/bets/interfaces";
import { formatCurrency } from "@/utils/format-currency";

interface AgentBankrollFlowChartProps {
  points: BankrollFlowPointDto[];
  isLoading: boolean;
}

function formatTimestampLabel(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString();
}

export function AgentBankrollFlowChart({ points, isLoading }: AgentBankrollFlowChartProps) {
  if (isLoading) {
    return (
      <div className="h-56 animate-pulse rounded-md bg-zinc-100 dark:bg-zinc-900" />
    );
  }

  if (points.length === 0) {
    return (
      <div className="flex h-56 items-center justify-center rounded-md border border-dashed border-zinc-300 text-sm text-zinc-500 dark:border-zinc-700 dark:text-zinc-400">
        No bankroll points available.
      </div>
    );
  }

  return (
    <div className="h-56">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={points}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} />
          <XAxis
            dataKey="timestamp"
            tickFormatter={formatTimestampLabel}
            minTickGap={24}
          />
          <YAxis tickFormatter={(value) => formatCurrency(Number(value))} width={92} />
          <Tooltip
            formatter={(value: number) => formatCurrency(value)}
            labelFormatter={(value) => `Date: ${formatTimestampLabel(String(value))}`}
          />
          <Line
            type="monotone"
            dataKey="balanceAfter"
            stroke="currentColor"
            strokeWidth={2}
            dot={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
