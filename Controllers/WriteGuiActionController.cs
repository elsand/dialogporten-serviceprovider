using Digdir.BDB.Dialogporten.ServiceProvider.Clients;
using Digdir.BDB.Dialogporten.ServiceProvider.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Controllers;

[ApiController]
[Route("guiaction/write")]
[Authorize(AuthenticationSchemes = "DialogToken")]
[EnableCors("AllowedOriginsPolicy")]
public class WriteGuiActionController : Controller
{
    private readonly IDialogporten _dialogporten;
    private readonly IBackgroundTaskQueue _taskQueue;

    public WriteGuiActionController(
        IDialogporten dialogporten,
        IBackgroundTaskQueue taskQueue)
    {
        _dialogporten = dialogporten;
        _taskQueue = taskQueue;
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromQuery]string xacmlaction = "write",
        [FromQuery]bool isDelayed = false,
        [FromQuery]bool addActivity = false,
        [FromQuery]bool addTransmission = false,
        [FromQuery]bool addAttachment = false,
        [FromQuery]DialogStatus_Values? setStatusTo = null,
        [FromQuery]bool? setDialogGuiActionsToDeleteOnly = false)
    {
        if (!HasPermission(xacmlaction))
        {
            return Forbid();
        }

        var dialogId = GetDialogId();
        var operations = new List<Operation>();

        if (addActivity)
        {
            operations.Add(new Operation
            {
                Op = "add",
                Path = "/activities/-",
                Value = new CreateDialogDialogActivityDto
                {
                    Type = DialogActivityType_Values.Information,
                    PerformedBy = new CreateDialogDialogActivityPerformedByActorDto
                    {
                        ActorType = ActorType_Values.PartyRepresentative,
                        ActorId = GetActorId()
                    },
                    Description = new List<LocalizationDto>
                    {
                        new () { LanguageCode = "en", Value = "Activity added by dialogporten-serviceprovider"}}
                    }
                }
            );
        }

        if (addTransmission)
        {
            operations.Add(new Operation
            {
                Op = "add",
                Path = "/transmissions/-",
                Value = new CreateDialogDialogTransmissionDto
                {
                    Type = DialogTransmissionType_Values.Information,
                    Sender = new CreateDialogDialogTransmissionSenderActorDto
                    {
                        ActorType = ActorType_Values.PartyRepresentative,
                        ActorId = GetActorId()
                    },
                    Content = new CreateDialogDialogTransmissionContentDto
                    {
                        Title = new ContentValueDto
                        {
                            MediaType = "text/plain", Value = new List<LocalizationDto>
                            {
                                new()
                                {
                                    LanguageCode = "en", Value = "Transmission title added by dialogporten-serviceprovider"
                                }
                            }
                        },
                        Summary = new ContentValueDto
                        {
                            MediaType = "text/plain", Value = new List<LocalizationDto>
                            {
                                new()
                                {
                                    LanguageCode = "en", Value = "Transmission summary added by dialogporten-serviceprovider"
                                }
                            }
                        },
                    }
                }
            });
        }

        if (addAttachment)
        {

        }

        if (setStatusTo.HasValue)
        {
            operations.Add(new Operation
            {
                Op = "replace",
                Path = "/status",
                Value = setStatusTo.Value
            });
        }

        if (isDelayed)
        {
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                await Task.Delay(1000, token);
                var result = await _dialogporten.Patch(dialogId, operations, null, token);
                if (!result.IsSuccessStatusCode)
                {
                    Console.WriteLine(result.Error.Content);
                }
            });

            return StatusCode(202);
        }

        await _dialogporten.Patch(dialogId, operations, null, CancellationToken.None);
        return Created();


        /*
        var paginatedList = await _dialogporten.GetDialogListSO(
            serviceResource: null!,
            party: null!,
            endUserId: null!,
            extendedStatus: null!,
            externalReference: null!,
            status: null!,
            createdAfter: null!,
            createdBefore: null!,
            updatedAfter: null!,
            updatedBefore: null!,
            dueAfter: null!,
            dueBefore: null!,
            visibleAfter: null!,
            visibleBefore: null!,
            process: null!,
            search: null!,
            searchLanguageCode: null!,
            orderBy: null!,
            continuationToken: null!,
            limit: null!,
            cancellationToken: CancellationToken.None
        );

        return Ok(paginatedList);
        */
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery]string xacmlaction = "write",
        [FromQuery]bool isDelayed = false)
    {

        if (!HasPermission(xacmlaction))
        {
            return Forbid();
        }

        var dialogId = GetDialogId();

        if (isDelayed)
        {
            // Start a background task to delete the dialog after 1 second
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                await Task.Delay(1000, token);
                await _dialogporten.DeleteDialog(dialogId, null, token);
            });

            return StatusCode(202);
        }

        // Delete the dialog immediately
        await _dialogporten.DeleteDialog(dialogId, null, CancellationToken.None);
        return NoContent();
    }

    private bool HasPermission(string xacmlaction)
    {
        return User.Claims.Any(c => c.Type == "a" && c.Value.Split(';').Any(x => x == xacmlaction));
    }

    private Guid GetDialogId()
    {
        var dialogId = User.Claims.FirstOrDefault(c => c.Type == "i")?.Value;
        if (dialogId is null)
        {
            throw new InvalidOperationException("Dialog id not found in token");
        }

        return Guid.Parse(dialogId);
    }

    public string GetActorId()
    {
        var actorId = User.Claims.FirstOrDefault(c => c.Type == "c")?.Value;
        if (actorId is null)
        {
            throw new InvalidOperationException("Actor id not found in token");
        }

        return actorId;
    }
}
