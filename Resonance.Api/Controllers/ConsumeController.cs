﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Resonance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Api.Controllers
{
    [Route("consume")]
    public class ConsumeController : Controller
    {
        private IEventConsumer _consumer;
        private ILogger<ConsumeController> _logger;

        public ConsumeController(IEventConsumer consumer, ILogger<ConsumeController> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        [HttpGet("{name}")]
        public IActionResult ConsumeNext(string name, int? visibilityTimeout, int? maxCount)
        {
            try
            {
                var ce = _consumer.ConsumeNext(name, visibilityTimeout.GetValueOrDefault(120), maxCount.GetValueOrDefault(1)).FirstOrDefault();
                if (ce == null)
                    return NotFound();
                else
                    return Ok(ce);
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode(500);
            }
        }
    }
}