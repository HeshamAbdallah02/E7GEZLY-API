using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetLinkedAccounts
{
    /// <summary>
    /// Handler for getting linked social accounts for a user
    /// </summary>
    public class GetLinkedAccountsHandler : IRequestHandler<GetLinkedAccountsQuery, OperationResult<LinkedAccountsResponseDto>>
    {
        private readonly AppDbContext _context;

        public GetLinkedAccountsHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<LinkedAccountsResponseDto>> Handle(GetLinkedAccountsQuery request, CancellationToken cancellationToken)
        {
            var linkedAccounts = await _context.ExternalLogins
                .Where(e => e.UserId == request.UserId)
                .Select(e => new LinkedAccountDto(
                    e.Provider,
                    e.ProviderEmail,
                    e.ProviderDisplayName,
                    e.CreatedAt,
                    e.LastLoginAt
                ))
                .ToListAsync(cancellationToken);

            var response = new LinkedAccountsResponseDto(linkedAccounts);
            
            return OperationResult<LinkedAccountsResponseDto>.Success(response);
        }
    }
}