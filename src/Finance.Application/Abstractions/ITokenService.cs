using Finance.Domain.Entities;

namespace Finance.Application.Abstractions;

public interface ITokenService
{
    string CreateToken(User user);
}
