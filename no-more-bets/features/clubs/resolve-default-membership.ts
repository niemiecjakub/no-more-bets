import type { ClubSeasonMembership } from "./interfaces";

export function resolveDefaultMembership(
  memberships: ClubSeasonMembership[],
): ClubSeasonMembership | null {
  if (memberships.length === 0) return null;
  const today = new Date().toISOString().slice(0, 10);
  return (
    memberships.find(
      (membership) =>
        (!membership.startDate || membership.startDate <= today) &&
        (!membership.endDate || membership.endDate >= today),
    ) ?? memberships[0]
  );
}
