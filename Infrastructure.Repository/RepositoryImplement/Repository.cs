﻿using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Common.RepositoryTool;

namespace Infrastructure.Repository.RepositoryImplement
{
    public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : EntityBase<TKey>
    {
        private IUnitOfWork unitOfWork { get; set; }
        private readonly DbSet<TEntity> dbset;
        private DbContext efContext;
        public Repository(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            efContext = this.unitOfWork.Get();
            dbset = efContext.Set<TEntity>();
        }

        public void Add(TEntity entity)
        {
            dbset.Add(entity);
        }

        public async Task AddAsync(TEntity entity)
        {
            await dbset.AddAsync(entity);
        }

        public void AddAll(IEnumerable<TEntity> entities)
        {
            dbset.AddRange(entities);
        }

        public async Task AddAllAsync(IEnumerable<TEntity> entities)
        {
            await dbset.AddRangeAsync(entities);
        }

        public void Delete(TEntity entity)
        {
            dbset.Remove(entity);
        }

        public async Task<int> DeleteAsync(TEntity entity)
        {
            dbset.Remove(entity);
            return await unitOfWork.CommitAsync();
        }

        public IQueryable<TEntity> GetAll()
        {
            return dbset;
        }

        public async Task<TEntity> GetByIdAsync(TKey id)
        {
            return await dbset.FindAsync(id);
        }

        public TEntity GetById(TKey id)
        {
            return dbset.Find(id);
        }

        public void Update(TEntity entity)
        {
            dbset.Update(entity);
        }

        public async Task<int> UpdateAsync(TEntity entity)
        {
            dbset.Update(entity);
            return await unitOfWork.CommitAsync();
        }
    }
}
