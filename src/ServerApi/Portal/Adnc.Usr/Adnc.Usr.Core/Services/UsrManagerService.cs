﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adnc.Infr.Common.Helper;
using Adnc.Usr.Core.Entities;
using Adnc.Core.Shared.IRepositories;
using Adnc.Core.Shared.Interceptors;
using Adnc.Core.Shared;

namespace Adnc.Usr.Core.Services
{
    public class UsrManagerService : ICoreService
    {
        private readonly IEfRepository<SysUser> _userRepository;
        private readonly IEfRepository<SysUserFinance> _financeRepository;
        private readonly IEfRepository<SysRelation> _relationRepository;
        private readonly IEfRepository<SysDept> _deptRepository;
        private readonly IEfRepository<SysMenu> _menuRepository;

        public UsrManagerService(IEfRepository<SysUser> userRepository
            , IEfRepository<SysUserFinance> financeRepository
            , IEfRepository<SysRelation> relationRepository
            , IEfRepository<SysMenu> menuRepository
            , IEfRepository<SysDept> deptRepository
            )
        {
            _userRepository = userRepository;
            _financeRepository = financeRepository;
            _relationRepository = relationRepository;
            _menuRepository = menuRepository;
            _deptRepository = deptRepository;
        }

        [UnitOfWork]
        public virtual async Task AddUser(SysUser user, CancellationToken cancellationToken = default)
        {
            await _userRepository.InsertAsync(user, cancellationToken);
            await _financeRepository.InsertAsync(new SysUserFinance { Id = user.Id, Amount = 0.00M }, cancellationToken);
        }

        [UnitOfWork]
        public virtual async Task SaveRolePermisson(long roleId, long[] permissionIds, CancellationToken cancellationToken = default)
        {
            await _relationRepository.DeleteRangeAsync(x => x.RoleId == roleId);

            var relations = new List<SysRelation>();
            foreach (var permissionId in permissionIds)
            {
                relations.Add(
                    new SysRelation
                    {
                        Id = IdGenerater.GetNextId(),
                        RoleId = roleId,
                        MenuId = permissionId
                    }
                );
            }
            await _relationRepository.InsertRangeAsync(relations);
        }

        [UnitOfWork]
        public virtual async Task UpdateDept(string oldDeptPids, SysDept dept, CancellationToken cancellationToken = default)
        {
            await _deptRepository.UpdateAsync(dept);
            //zz.efcore 不支持
            //await _deptRepository.UpdateRangeAsync(d => d.Pids.Contains($"[{dept.ID}]"), c => new SysDept { Pids = c.Pids.Replace(oldDeptPids, dept.Pids) });
            var originalDeptPids = $"{oldDeptPids}[{dept.Id}],";
            var nowDeptPids = $"{dept.Pids}[{dept.Id}],";

            var subDepts = await _deptRepository
                                 .Where(d => d.Pids.StartsWith(originalDeptPids))
                                 .Select(d => new { d.Id, d.Pids })
                                 .ToListAsync();

            //var subDepts = await _deptRepository.SelectAsync(d => new { d.Id, d.Pids }
            //                                                , d => d.Pids.StartsWith(originalDeptPids)
            //                                                );
            subDepts.ForEach(c =>
            {
                _deptRepository.UpdateAsync(new SysDept { Id = c.Id, Pids = c.Pids.Replace(originalDeptPids, nowDeptPids) }, c => c.Pids).Wait();
            });
        }
    }
}
