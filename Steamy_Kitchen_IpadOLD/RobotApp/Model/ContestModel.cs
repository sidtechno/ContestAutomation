using System;
using System.Collections.Generic;

namespace RobotApp.Model
{
    public class ContestModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime EndDate { get; set; }
        public IEnumerable<ContestActionModel> Actions { get; set; }
        public ContestResultModel ContestResult { get; set; }
    }
}