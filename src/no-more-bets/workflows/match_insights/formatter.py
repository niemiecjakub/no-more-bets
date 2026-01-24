"""Formatters for match insights output."""
import json
from typing import Optional
from .models import MatchAnalysisReport


class ConsoleReportFormatter:
    """Formatter for console output of match analysis reports."""
    
    @staticmethod
    def format(report: MatchAnalysisReport) -> str:
        """Format match analysis report for console output.
        
        Parameters
        ----------
        report : MatchAnalysisReport
            The report to format.
            
        Returns
        -------
        str
            Formatted string for console output.
        """
        lines = []
        lines.append("=" * 80)
        lines.append(f"MATCH ANALYSIS REPORT")
        lines.append("=" * 80)
        lines.append("")
        lines.append(f"{report.match_info.home} (H) vs {report.match_info.away} (A)")
        lines.append(f"Date: {report.match_info.date} @ {report.match_info.time}")
        lines.append("")
        
        # Key Insights
        if report.key_insights:
            lines.append("KEY INSIGHTS:")
            for i, insight in enumerate(report.key_insights, 1):
                lines.append(f"  {i}. {insight}")
            lines.append("")
        
        # Home Team Analysis
        lines.append(f"HOME TEAM: {report.home_team_analysis.team_name}")
        lines.append(f"  Form: {report.home_team_analysis.form_summary}")
        if report.home_team_analysis.key_players:
            lines.append(f"  Key Players: {', '.join(report.home_team_analysis.key_players)}")
        lines.append(f"  Injury Impact: {report.home_team_analysis.injury_impact}")
        if report.home_team_analysis.statistical_strengths:
            lines.append(f"  Strengths: {', '.join(report.home_team_analysis.statistical_strengths)}")
        if report.home_team_analysis.statistical_weaknesses:
            lines.append(f"  Weaknesses: {', '.join(report.home_team_analysis.statistical_weaknesses)}")
        lines.append("")
        
        # Away Team Analysis
        lines.append(f"AWAY TEAM: {report.away_team_analysis.team_name}")
        lines.append(f"  Form: {report.away_team_analysis.form_summary}")
        if report.away_team_analysis.key_players:
            lines.append(f"  Key Players: {', '.join(report.away_team_analysis.key_players)}")
        lines.append(f"  Injury Impact: {report.away_team_analysis.injury_impact}")
        if report.away_team_analysis.statistical_strengths:
            lines.append(f"  Strengths: {', '.join(report.away_team_analysis.statistical_strengths)}")
        if report.away_team_analysis.statistical_weaknesses:
            lines.append(f"  Weaknesses: {', '.join(report.away_team_analysis.statistical_weaknesses)}")
        lines.append("")
        
        # Statistical Summary
        lines.append("STATISTICAL SUMMARY:")
        lines.append(f"  Win Probability - Home: {report.statistical_summary.win_probability_home:.1%}")
        lines.append(f"  Win Probability - Away: {report.statistical_summary.win_probability_away:.1%}")
        lines.append(f"  Draw Probability: {report.statistical_summary.draw_probability:.1%}")
        lines.append(f"  Expected Goals: {report.statistical_summary.expected_goals:.2f}")
        if report.statistical_summary.injury_adjusted_strength:
            for team, strength in report.statistical_summary.injury_adjusted_strength.items():
                lines.append(f"  {team.capitalize()} Strength (injury-adjusted): {strength:.2f}")
        lines.append("")
        
        # Head-to-Head Insights
        if report.head_to_head_insights:
            lines.append("HEAD-TO-HEAD INSIGHTS:")
            for i, insight in enumerate(report.head_to_head_insights, 1):
                lines.append(f"  {i}. {insight}")
            lines.append("")
        
        # Match Context
        if report.match_context:
            lines.append("MATCH CONTEXT:")
            lines.append(f"  {report.match_context}")
            lines.append("")
        
        lines.append("=" * 80)
        
        return "\n".join(lines)


class JsonReportFormatter:
    """Formatter for JSON output of match analysis reports."""
    
    @staticmethod
    def format(report: MatchAnalysisReport, indent: int = 2) -> str:
        """Format match analysis report as JSON.
        
        Parameters
        ----------
        report : MatchAnalysisReport
            The report to format.
        indent : int
            JSON indentation level.
            
        Returns
        -------
        str
            JSON string representation.
        """
        return report.model_dump_json(indent=indent)
    
    @staticmethod
    def save(report: MatchAnalysisReport, filepath: str, indent: int = 2) -> None:
        """Save match analysis report to JSON file.
        
        Parameters
        ----------
        report : MatchAnalysisReport
            The report to save.
        filepath : str
            Path to save the JSON file.
        indent : int
            JSON indentation level.
        """
        json_str = JsonReportFormatter.format(report, indent=indent)
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(json_str)
