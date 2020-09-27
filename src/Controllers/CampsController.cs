using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CoreCodeCamp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampsController : ControllerBase
    {
        private readonly ICampRepository repository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;
        private readonly ILogger logger;

        public CampsController(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator, ILogger<CampsController> logger)
        {
            this.repository = repository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<CampModel[]>> Get(bool includeTalks = false)
        {
            try
            {
                var results = await this.repository.GetAllCampsAsync(includeTalks);
                return this.mapper.Map<CampModel[]>(results);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }

        [HttpGet("{moniker}")]
        public async Task<ActionResult<CampModel>> Get(string moniker)
        {
            try
            {
                var result = await this.repository.GetCampAsync(moniker);
                if (result == null) return NotFound();
                return this.mapper.Map<CampModel>(result);
            }
            catch
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<CampModel[]>> SearchByDate(DateTime theDate, bool includeTalks = false)
        {
            try
            {
                var results = await this.repository.GetAllCampsByEventDate(theDate, includeTalks);
                if (!results.Any()) return NotFound();
                return this.mapper.Map<CampModel[]>(results);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }


        }

        public async Task<ActionResult<CampModel>> Post(CampModel model)
        {
            try
            {
                var existing = await this.repository.GetCampAsync(model.Moniker);
                if (existing != null)
                {
                    return BadRequest("Moniker in Use");
                }
                var location = this.linkGenerator.GetPathByAction("Get", "Camps",
                    new { moniker = model.Moniker });

                if (string.IsNullOrWhiteSpace(location))
                {
                    return BadRequest("Could not use the current moniker");
                }

                var camp = this.mapper.Map<Camp>(model);
                this.repository.Add(camp);
                if (await this.repository.SaveChangesAsync())
                {
                    return Created($"/api/camps/{camp.Moniker}", this.mapper.Map<CampModel>(camp));
                }
                return Ok();
            }
            catch (Exception e)
            {
                this.logger.LogError($"{e}");
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }

        [HttpPut("{moniker}")]
        public async Task<ActionResult<CampModel>> Put(string moniker, CampModel model)
        {
            try
            {
                var oldCamp = await this.repository.GetCampAsync(model.Moniker);
                if (oldCamp == null) return NotFound($"Could not find camp with moniker of {moniker}");

                this.mapper.Map(model, oldCamp);

                if (await this.repository.SaveChangesAsync())
                {
                    return this.mapper.Map<CampModel>(oldCamp);
                }
                return NotFound("Something failed.");
            }
            catch (Exception e)
            {
                this.logger.LogError($"{e}");
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }
    }
}
