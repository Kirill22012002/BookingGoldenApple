using BGA.API.Presentation.Dtos;
using BGA.API.Application.Extensions;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class EventService(IEventRepository _repository) : IEventService
{
    public ServiceResponse<PaginatedResult<EventDto>> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        try
        {
            var query = _repository.GetAll();
            if (title != null) query = query.Where(@event => @event.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            if (from != null) query = query.Where(@event => @event.StartAt >= from);
            if (to != null) query = query.Where(@event => @event.EndAt <= to);

            var filteredCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var paginatedResult = new PaginatedResult<EventDto>()
            {
                Items = items.AsEnumerable().MapToDtos(),
                TotalItems = filteredCount,
                PageNumber = page,
                PageSize = items.Count()
            };

            return ServiceResponse<PaginatedResult<EventDto>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PaginatedResult<EventDto>>.Failure([ex.Message]);
        }
    }

    public ServiceResponse<EventDto> GetById(int id)
    {
        try
        {
            var @event = _repository.GetById(id);
            return ServiceResponse<EventDto>.Success(@event.MapToDto());
        }
        catch (Exception ex)
        {
            return ServiceResponse<EventDto>.Failure([ex.Message]);
        }
    }

    public ServiceResponse<EventDto> Create(AddEventDto dto)
    {
        try
        {
            var @event = dto.MapToEntity();
            var success = _repository.Create(@event);

            return success
                ? ServiceResponse<EventDto>.Success(@event.MapToDto())
                : ServiceResponse<EventDto>.Failure(["Cannot create event"]);

        }
        catch (Exception ex)
        {
            return ServiceResponse<EventDto>.Failure([ex.Message]);
        }
    }

    public ServiceResponse Update(int id, PutEventDto dto)
    {
        try
        {
            var @event = dto.MapToEntity(id);
            var success = _repository.Update(id, @event);

            return success
                ? ServiceResponse.Success()
                : ServiceResponse.Failure(["Cannot create event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Failure([ex.Message]);
        }
    }

    public ServiceResponse Remove(int id)
    {
        try
        {
            var success = _repository.Remove(id);

            return success
                ? ServiceResponse.Success()
                : ServiceResponse.Failure(["Cannot remove event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Failure([ex.Message]);
        }
    }
}
