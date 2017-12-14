﻿using System;
using System.Collections.Generic;
using System.Linq;
using NPoco;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Models.Rdbms;
using Umbraco.Core.Persistence.Factories;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Scoping;

namespace Umbraco.Core.Persistence.Repositories.Implement
{
    /// <summary>
    /// Represents a repository for doing CRUD operations for <see cref="RelationType"/>
    /// </summary>
    internal class RelationTypeRepository : NPocoRepositoryBase<int, IRelationType>, IRelationTypeRepository
    {
        public RelationTypeRepository(IScopeAccessor scopeAccessor, CacheHelper cache, ILogger logger)
            : base(scopeAccessor, cache, logger)
        { }

        protected override IRepositoryCachePolicy<IRelationType, int> CreateCachePolicy(IRuntimeCacheProvider runtimeCache)
        {
            return new FullDataSetRepositoryCachePolicy<IRelationType, int>(runtimeCache, GetEntityId, /*expires:*/ true);
        }

        #region Overrides of RepositoryBase<int,RelationType>

        protected override IRelationType PerformGet(int id)
        {
            // use the underlying GetAll which will force cache all content types
            return GetMany().FirstOrDefault(x => x.Id == id);
        }

        public IRelationType Get(Guid id)
        {
            // use the underlying GetAll which will force cache all content types
            return GetMany().FirstOrDefault(x => x.Key == id);
        }

        public bool Exists(Guid id)
        {
            return Get(id) != null;
        }

        protected override IEnumerable<IRelationType> PerformGetAll(params int[] ids)
        {
            var sql = GetBaseQuery(false);

            // should not happen due to the cache policy
            if (ids.Any())
                throw new NotImplementedException();

            var dtos = Database.Fetch<RelationTypeDto>(sql);
            var factory = new RelationTypeFactory();
            return dtos.Select(x => DtoToEntity(x, factory));
        }

        public IEnumerable<IRelationType> GetMany(params Guid[] ids)
        {
            // should not happen due to the cache policy
            if (ids.Any())
                throw new NotImplementedException();

            return GetMany(new int[0]);
        }

        protected override IEnumerable<IRelationType> PerformGetByQuery(IQuery<IRelationType> query)
        {
            var sqlClause = GetBaseQuery(false);
            var translator = new SqlTranslator<IRelationType>(sqlClause, query);
            var sql = translator.Translate();

            var dtos = Database.Fetch<RelationTypeDto>(sql);
            var factory = new RelationTypeFactory();
            return dtos.Select(x => DtoToEntity(x, factory));
        }

        private static IRelationType DtoToEntity(RelationTypeDto dto, RelationTypeFactory factory)
        {
            var entity = factory.BuildEntity(dto);

            // reset dirty initial properties (U4-1946)
            ((TracksChangesEntityBase) entity).ResetDirtyProperties(false);

            return entity;
        }

        #endregion

        #region Overrides of NPocoRepositoryBase<int,RelationType>

        protected override Sql<ISqlContext> GetBaseQuery(bool isCount)
        {
            var sql = Sql();

            sql = isCount
                ? sql.SelectCount()
                : sql.Select<RelationTypeDto>();

            sql
               .From<RelationTypeDto>();

            return sql;
        }

        protected override string GetBaseWhereClause()
        {
            return "umbracoRelationType.id = @id";
        }

        protected override IEnumerable<string> GetDeleteClauses()
        {
            var list = new List<string>
                           {
                               "DELETE FROM umbracoRelation WHERE relType = @id",
                               "DELETE FROM umbracoRelationType WHERE id = @id"
                           };
            return list;
        }

        protected override Guid NodeObjectTypeId
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region Unit of Work Implementation

        protected override void PersistNewItem(IRelationType entity)
        {
            ((Entity)entity).AddingEntity();

            var factory = new RelationTypeFactory();
            var dto = factory.BuildDto(entity);

            var id = Convert.ToInt32(Database.Insert(dto));
            entity.Id = id;

            entity.ResetDirtyProperties();
        }

        protected override void PersistUpdatedItem(IRelationType entity)
        {
            ((Entity)entity).UpdatingEntity();

            var factory = new RelationTypeFactory();
            var dto = factory.BuildDto(entity);
            Database.Update(dto);

            entity.ResetDirtyProperties();
        }

        #endregion
    }
}
