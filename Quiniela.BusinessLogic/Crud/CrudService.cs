using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public abstract class CrudService<TEntity> where TEntity : class
{
    private readonly QuinielaDbContext context;
    private readonly DbSet<TEntity> entities;

    protected CrudService(QuinielaDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        entities = context.Set<TEntity>();
    }

    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await entities.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entities.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteAsync(object[] keyValues, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyValues);

        TEntity? entity = await entities.FindAsync(keyValues, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entities.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TEntity?> GetByPrimaryKeyAsync(object[] keyValues, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyValues);

        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} is not part of the EF model.");
        IKey primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} has no primary key.");
        Expression<Func<TEntity, bool>> keyPredicate = BuildPrimaryKeyPredicate(primaryKey, keyValues);

        return await IncludeNavigationProperties(entities)
            .SingleOrDefaultAsync(keyPredicate, cancellationToken);
    }

    public async Task<bool> ExistsByPrimaryKeyAsync(
        object[] keyValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyValues);

        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} is not part of the EF model.");
        IKey primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} has no primary key.");
        Expression<Func<TEntity, bool>> keyPredicate = BuildPrimaryKeyPredicate(primaryKey, keyValues);

        return await entities.AnyAsync(keyPredicate, cancellationToken);
    }

    public async Task<List<TEntity>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        int pageOffset = checked((pageNumber - 1) * pageSize);
        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} is not part of the EF model.");
        IKey primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} has no primary key.");
        IQueryable<TEntity> query = OrderByPrimaryKey(
            IncludeNavigationProperties(entities.AsNoTracking()),
            primaryKey);

        return await query
            .Skip(pageOffset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<TEntity> IncludeNavigationProperties(IQueryable<TEntity> query)
    {
        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} is not part of the EF model.");

        foreach (string navigationName in entityType.GetNavigations()
            .Select(navigation => navigation.Name)
            .Concat(entityType.GetSkipNavigations().Select(navigation => navigation.Name)))
        {
            query = query.Include(navigationName);
        }

        return query;
    }

    private static IQueryable<TEntity> OrderByPrimaryKey(IQueryable<TEntity> query, IKey primaryKey)
    {
        ParameterExpression entityParameter = Expression.Parameter(typeof(TEntity), "entity");
        Expression orderedExpression = query.Expression;

        for (int index = 0; index < primaryKey.Properties.Count; index++)
        {
            IProperty keyProperty = primaryKey.Properties[index];
            MethodCallExpression propertyAccess = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { keyProperty.ClrType },
                entityParameter,
                Expression.Constant(keyProperty.Name));
            LambdaExpression keySelector = Expression.Lambda(propertyAccess, entityParameter);
            orderedExpression = Expression.Call(
                typeof(Queryable),
                index == 0 ? nameof(Queryable.OrderBy) : nameof(Queryable.ThenBy),
                new[] { typeof(TEntity), keyProperty.ClrType },
                orderedExpression,
                Expression.Quote(keySelector));
        }

        return query.Provider.CreateQuery<TEntity>(orderedExpression);
    }

    private static Expression<Func<TEntity, bool>> BuildPrimaryKeyPredicate(IKey primaryKey, object[] keyValues)
    {
        if (keyValues.Length != primaryKey.Properties.Count)
        {
            throw new ArgumentException(
                $"Expected {primaryKey.Properties.Count} key value(s), but received {keyValues.Length}.",
                nameof(keyValues));
        }

        var entityParameter = Expression.Parameter(typeof(TEntity), "entity");
        Expression? predicateBody = null;

        for (int index = 0; index < primaryKey.Properties.Count; index++)
        {
            IProperty keyProperty = primaryKey.Properties[index];
            object keyValue = keyValues[index]
                ?? throw new ArgumentException("Primary key values cannot be null.", nameof(keyValues));

            if (!keyProperty.ClrType.IsInstanceOfType(keyValue))
            {
                throw new ArgumentException(
                    $"Key value at position {index} must be of type {keyProperty.ClrType.Name}.",
                    nameof(keyValues));
            }

            MethodCallExpression propertyAccess = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { keyProperty.ClrType },
                entityParameter,
                Expression.Constant(keyProperty.Name));
            BinaryExpression keyMatch = Expression.Equal(
                propertyAccess,
                Expression.Constant(keyValue, keyProperty.ClrType));

            predicateBody = predicateBody is null
                ? keyMatch
                : Expression.AndAlso(predicateBody, keyMatch);
        }

        return Expression.Lambda<Func<TEntity, bool>>(predicateBody!, entityParameter);
    }
}
