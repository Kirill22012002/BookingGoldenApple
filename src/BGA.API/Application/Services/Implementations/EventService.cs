using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Repositories.Interfaces;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Implementations;

public class EventService(IEventRepository _repository) : IEventService
{
    public ServiceResponse<PaginatedResult<Event>> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize)
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

            var paginatedResult = new PaginatedResult<Event>()
            {
                Items = items.AsEnumerable(),
                TotalItems = filteredCount,
                PageNumber = page,
                PageSize = items.Count()
            };

            return ServiceResponse<PaginatedResult<Event>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PaginatedResult<Event>>.Failure([ex.Message]);
        }
    }

    public ServiceResponse<Event> GetById(int id)
    {
        try
        {
            var @event = _repository.GetById(id);
            return ServiceResponse<Event>.Success(@event);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Event>.Failure([ex.Message]);
        }
    }

    public ServiceResponse<Event> Create(Event @event)
    {
        try
        {
            var success = _repository.Create(@event);

            return success
                ? ServiceResponse<Event>.Success(@event)
                : ServiceResponse<Event>.Failure(["Cannot create event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Event>.Failure([ex.Message]);
        }
    }

    public ServiceResponse Update(int id, Event @event)
    {
        try
        {
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
