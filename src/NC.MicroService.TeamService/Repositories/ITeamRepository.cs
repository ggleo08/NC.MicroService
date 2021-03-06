﻿using NC.MicroService.EntityFrameworkCore.Repository;
using NC.MicroService.TeamService.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NC.MicroService.TeamService.Repositories
{
    /// <summary>
    /// 团队模型仓储接口
    /// </summary>
    public interface ITeamRepository : IRepository<Team, Guid>
    {
    }
}
