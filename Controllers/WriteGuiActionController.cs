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
        [FromQuery]bool queueInBackground = false,
        [FromQuery]bool addActivity = false,
        [FromQuery]bool addTransmission = false,
        [FromQuery]bool addAttachment = false,
        [FromQuery]bool setDialogGuiActionsToDeleteOnly = false,
        [FromQuery]DialogStatus_Values? setStatusTo = null)
    {
        if (!IsAuthorized(xacmlaction))
        {
            return Forbid();
        }

        var operations = new List<Operation>();

        if (addActivity)
        {
            operations.Add(GetAddActivityOp());
        }

        if (addTransmission)
        {
            operations.Add(GetAddTransmissionOp());
        }

        if (addAttachment)
        {
            operations.Add(GetAddAttachmentOp());
        }

        if (setStatusTo.HasValue)
        {
            operations.Add(GetReplaceStatusOp(setStatusTo.Value));
        }

        if (setDialogGuiActionsToDeleteOnly)
        {
            operations.Add(GetReplaceGuiActionsOp());
        }

        return await PerformMaybeBackgroundOperation(queueInBackground, () =>
            _dialogporten.Patch(GetDialogId(), operations, null, CancellationToken.None));

    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery]string xacmlaction = "write",
        [FromQuery]bool queueInBackground = false)
    {

        if (!IsAuthorized(xacmlaction))
        {
            return Forbid();
        }

        return await PerformMaybeBackgroundOperation(queueInBackground, () =>
            _dialogporten.DeleteDialog(GetDialogId(), null, CancellationToken.None));
    }

    private Operation GetReplaceGuiActionsOp()
    {
        return new Operation
        {
            Op = "replace",
            Path = "/guiActions",
            Value = new List<UpdateDialogDialogGuiActionDto>
            {
                new()
                {
                    Action = "write",
                    IsDeleteDialogAction = true,
                    HttpMethod = HttpVerb_Values.DELETE,
                    Title = new List<LocalizationDto> { new() { LanguageCode = "en", Value = "Delete dialog" } },
                    Url = GetActionUrl(typeof(WriteGuiActionController), nameof(Delete))
                }
            }
        };
    }

    private static Operation GetReplaceStatusOp(DialogStatus_Values setStatusTo)
    {
        return new Operation
        {
            Op = "replace",
            Path = "/status",
            Value = setStatusTo
        };
    }

    private Operation GetAddAttachmentOp()
    {
        return new Operation
        {
            Op = "add",
            Path = "/attachments/-",
            Value = new UpdateDialogDialogAttachmentDto
            {
                DisplayName = new List<LocalizationDto>
                {
                    new()
                    {
                        LanguageCode = "en", Value = "Attachment added by dialogporten-serviceprovider"
                    }
                },
                Urls = new List<UpdateDialogDialogAttachmentUrlDto>
                {
                    new ()
                    {
                        ConsumerType = AttachmentUrlConsumerType_Values.Gui,
                        MediaType = "application/pdf",
                        Url = GetActionUrl(typeof(AttachmentController), nameof(AttachmentController.Get), new { fileName = "document.pdf" })
                    }
                }
            }
        };
    }

    private Operation GetAddTransmissionOp()
    {
        return new Operation
        {
            Op = "add",
            Path = "/transmissions/-",
            Value = new UpdateDialogDialogTransmissionDto
            {
                Type = DialogTransmissionType_Values.Information,
                Sender = new UpdateDialogDialogTransmissionSenderActorDto
                {
                    ActorType = ActorType_Values.PartyRepresentative,
                    ActorId = GetActorId()
                },
                Content = new UpdateDialogDialogTransmissionContentDto
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
        };
    }

    private Operation GetAddActivityOp()
    {
        return new Operation
        {
            Op = "add",
            Path = "/activities/-",
            Value = new UpdateDialogDialogActivityDto
            {
                Type = DialogActivityType_Values.Information,
                PerformedBy = new UpdateDialogDialogActivityPerformedByActorDto
                {
                    ActorType = ActorType_Values.PartyRepresentative,
                    ActorId = GetActorId()
                },
                Description = new List<LocalizationDto>
                {
                    new() { LanguageCode = "en", Value = "Activity added by dialogporten-serviceprovider" }
                }
            }
        };
    }

    private bool IsAuthorized(string xacmlaction)
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

    private async Task<IActionResult> PerformMaybeBackgroundOperation(bool isDelayed, Func<Task> operation)
    {
        if (isDelayed)
        {
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                await Task.Delay(1000, token);
                await operation();
            });

            return StatusCode(202);
        }

        await operation();
        return NoContent();
    }

    private Uri GetActionUrl(Type controllerType, string actionName, object? parameters = null)
    {
        var actionUrl = Url.Action(
            action: actionName,
            controller: controllerType.Name.Replace("Controller", ""),
            values: parameters,
            protocol: Request.Scheme,
            host: Request.Host.ToString()
        );

        return new Uri(actionUrl!);
    }
}
