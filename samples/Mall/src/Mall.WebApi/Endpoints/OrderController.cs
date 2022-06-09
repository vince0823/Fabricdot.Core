﻿using System;
using System.Threading.Tasks;
using Fabricdot.WebApi.Endpoint;
using Mall.WebApi.Application.Commands.Orders;
using Mall.WebApi.Application.Queries.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Mall.WebApi.Endpoints;

public class OrderController : EndPointBase
{
    [HttpPost]
    public async Task<Guid> CreateAsync(PlaceOrderCommand command)
    {
        return await Sender.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<OrderDetailsDto> GetDetails([FromRoute] Guid id)
    {
        return await Sender.Send(new GetOrderDetailsQuery(id));
    }
}