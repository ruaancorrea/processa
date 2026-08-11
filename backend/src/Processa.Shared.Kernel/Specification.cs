using System.Linq.Expressions;

namespace Processa.Shared.Kernel;

/// <summary>
/// Specification pattern para regras de negócio e critérios de consulta reutilizáveis/combináveis,
/// sem vazar detalhe de ORM para a camada de Application.
/// </summary>
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity) => ToExpression().Compile().Invoke(entity);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    public Specification<T> Not() => new NotSpecification<T>(this);
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();

        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param));

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var innerExpr = inner.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(innerExpr, param));

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
