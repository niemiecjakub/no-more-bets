using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoMoreBets.Infrastructure.Scraping.External.SoccerData;
public class SoccerDataOptions
{
  public const string SectionName = "SoccerData";

  public string? ApiKey { get; set; }
}
