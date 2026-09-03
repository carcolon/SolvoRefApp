using System.Net;
using System.ComponentModel.DataAnnotations;
using Core.Contracts.Fabric;
using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Feature.Fabric.GetValidateReferred;
using Core.Feature.Referrals.Common;
using Core.Feature.Referrals.CreateReferral;
using Core.Feature.Referrals.UpdateReferralStatus;
using Core.Models.Fabric;
using Core.Models.DataSourcing;
using Core.Models.Global;
using Core.Models.Identity;
using Core.Models.Referrals;
using Core.Repositories;
using Core.Security;
using Core.Service.DataSourcing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;

namespace Test;

public class SecurityRegressionTests
{
    [Fact]
    public void AuthSource_DoesNotExposeJwtInRedirectAndSupportsBearerFallback()
    {
        var root = FindRepositoryRoot();
        var loginHandler = File.ReadAllText(Path.Combine(root, "Core", "Feature", "Login", "LoginUser", "LoginUserRequestHandler.cs"));
        var authController = File.ReadAllText(Path.Combine(root, "Api", "Controller", "AuthController.cs"));

        loginHandler.ShouldNotContain("redirectUriFront");
        loginHandler.ShouldNotContain("?{data.Token}");
        authController.ShouldContain("return Redirect(redirectUriFront);");
        authController.ShouldContain("AppendAuthCookie(token");
        authController.ShouldContain("HttpOnly = true");
        authController.ShouldContain("Secure = true");
        authController.ShouldContain("CreateAuthResponseData(token, expires)");
        authController.ShouldContain("accessToken = token");
        authController.ShouldNotContain("?{data.Token}");
    }

    [Fact]
    public void EmployeeIdDiagnosticEndpoint_RequiresAuthenticationAndReadsStoredEmployeeId()
    {
        var root = FindRepositoryRoot();
        var authController = File.ReadAllText(Path.Combine(root, "Api", "Controller", "AuthController.cs"));
        var normalizedController = authController.Replace("\r\n", "\n");

        normalizedController.ShouldContain("[Authorize]\n        [HttpGet(\"diagnostics/employee-id\")]");
        authController.ShouldContain("public async Task<ActionResult<Response<object>>> GetEmployeeIdDiagnostic()");
        authController.ShouldContain("var userId = User.FindFirst(\"uid\")?.Value;");
        authController.ShouldContain("user.EmployeeId");
    }

    [Fact]
    public void RateLimitSource_Keeps429AndCriticalEndpointPolicies()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "Api", "Program.cs"));
        var fabricController = File.ReadAllText(Path.Combine(root, "Api", "Controller", "FabricController.cs"));
        var referralController = File.ReadAllText(Path.Combine(root, "Api", "Controller", "ReferralController.cs"));

        program.ShouldContain("RejectionStatusCode = StatusCodes.Status429TooManyRequests");
        program.ShouldContain("AddPolicy(\"referral-create\"");
        program.ShouldContain("AddPolicy(\"fabric-validate\"");
        program.ShouldContain("app.UseRateLimiter();");
        fabricController.ShouldContain("[EnableRateLimiting(\"fabric-validate\")]");
        referralController.ShouldContain("[EnableRateLimiting(\"referral-create\")]");
    }

    [Fact]
    public void CreateReferralDto_Sanitize_RemovesExecutableHtmlFromAllFields()
    {
        var dto = new CreateReferralDto
        {
            FirstName = "<img src=x onerror=alert(1)>Jane<script>alert(1)</script>",
            LastName = "<b>Doe</b>",
            Email = " jane@example.com<script>alert(1)</script> ",
            CountryCode = "<svg/onload=alert(1)>57",
            Phone = "300<script>alert(1)</script>123",
            Area = "<iframe src=evil></iframe>Sales",
            ReferralID = "ABC<script>alert(1)</script>",
            Experience = "<style>*{}</style>Senior",
            EnglishLevel = "<a href=javascript:alert(1)>B2</a>",
            Country = "<math><mi>x</mi></math>Colombia",
            City = "<span onclick=alert(1)>Bogota</span>",
            Account = "<object data=x></object>Account",
            HowHear = "<button onclick=alert(1)>LinkedIn</button>",
            Comments = "hello<script>alert(1)</script>\nnext<img src=x onerror=alert(1)>",
            VacancyId = "<meta http-equiv=refresh>VAC",
            ExternalVacancyId = "<meta http-equiv=refresh>JPC - 123",
            Position = "<select><option>Agent</option></select>",
            VacancyCountry = "<span onclick=alert(1)>Colombia</span>"
        };

        var sanitized = dto.Sanitize();
        var values = new[]
        {
            sanitized.FirstName,
            sanitized.LastName,
            sanitized.Email,
            sanitized.CountryCode,
            sanitized.Phone,
            sanitized.Area,
            sanitized.ReferralID,
            sanitized.Experience,
            sanitized.EnglishLevel,
            sanitized.Country,
            sanitized.City,
            sanitized.Account,
            sanitized.HowHear,
            sanitized.Comments,
            sanitized.VacancyId,
            sanitized.ExternalVacancyId,
            sanitized.Position,
            sanitized.VacancyCountry
        };

        foreach (var value in values)
        {
            var safeValue = value ?? string.Empty;
            safeValue.ShouldNotContain("<");
            safeValue.ShouldNotContain(">");
            safeValue.ShouldNotContain("script", Case.Insensitive);
            safeValue.ShouldNotContain("onerror", Case.Insensitive);
            safeValue.ShouldNotContain("javascript:", Case.Insensitive);
        }
    }

    [Fact]
    public void CreateReferralDto_Sanitize_DecodesPlainTextHtmlEntities()
    {
        var dto = new CreateReferralDto
        {
            Area = "Accounting &amp; Financial",
            Comments = "Accounting &amp; Financial"
        };

        var sanitized = dto.Sanitize();

        sanitized.Area.ShouldBe("Accounting & Financial");
        sanitized.Comments.ShouldBe("Accounting & Financial");
    }

    [Fact]
    public void FileUploadValidator_AllowsOnlyExpectedImageTypesWithMatchingMagicBytes()
    {
        var validPng = CreateFormFile("image.png", "image/png", [137, 80, 78, 71, 13, 10, 26, 10, 0, 0]);
        var spoofedJpg = CreateFormFile("image.jpg", "image/jpeg", "<script>alert(1)</script>"u8.ToArray());
        var gif = CreateFormFile("image.gif", "image/gif", "GIF89a"u8.ToArray());
        var unsafeSvg = CreateFormFile("image.svg", "image/svg+xml", "<svg><script>alert(1)</script></svg>"u8.ToArray());
        var mismatchedMime = CreateFormFile("image.webp", "application/octet-stream", [82, 73, 70, 70, 0, 0, 0, 0, 87, 69, 66, 80]);

        FileUploadValidator.ValidateImage(validPng).ShouldBeEmpty();
        FileUploadValidator.ValidateImage(spoofedJpg).ShouldContain("The uploaded file signature is invalid.");
        FileUploadValidator.ValidateImage(gif).ShouldBeEmpty();
        FileUploadValidator.ValidateImage(unsafeSvg).ShouldContain("The uploaded file signature is invalid.");
        FileUploadValidator.ValidateImage(mismatchedMime).ShouldContain("The uploaded file content type is not allowed.");
    }

    [Fact]
    public async Task GetValidateReferred_RejectsMalformedReferralIdBeforeCallingFabric()
    {
        var fabric = new Mock<IFabricService>(MockBehavior.Strict);
        var referrals = new Mock<IReferralRepository>(MockBehavior.Strict);

        var handler = new GetValidateReferredRequestHandler(fabric.Object, referrals.Object);

        var response = await handler.Handle(
            new GetValidateReferredRequest("3001234567", "candidate@example.com", "FAKE-ID"),
            CancellationToken.None);

        response.Success.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Errors!.ShouldContain("ReferralId not recognized.");
        fabric.Verify(x => x.ReferredValidation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        referrals.Verify(x => x.ExistsCandidateReferral(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetValidateReferred_ForValidNewReferralId_CallsFabricValidation()
    {
        var fabric = new Mock<IFabricService>(MockBehavior.Strict);
        var referrals = new Mock<IReferralRepository>(MockBehavior.Strict);
        referrals.Setup(x => x.ExistsCandidateReferral("68141611", "candidate@example.com")).ReturnsAsync(false);
        fabric.Setup(x => x.ReferredValidation("3001234567", "candidate@example.com"))
            .ReturnsAsync(Response<bool>.SuccessResponse(true, HttpStatusCode.OK));

        var handler = new GetValidateReferredRequestHandler(fabric.Object, referrals.Object);

        var response = await handler.Handle(
            new GetValidateReferredRequest("3001234567", "candidate@example.com", "68141611"),
            CancellationToken.None);

        response.Success.ShouldBeTrue();
        response.Data!.Validation.ShouldBeTrue();
        fabric.Verify(x => x.ReferredValidation("3001234567", "candidate@example.com"), Times.Once);
    }

    [Fact]
    public async Task GetValidateReferred_ForAlphaNumericReferralId_CallsFabricValidation()
    {
        var fabric = new Mock<IFabricService>(MockBehavior.Strict);
        var referrals = new Mock<IReferralRepository>(MockBehavior.Strict);
        referrals.Setup(x => x.ExistsCandidateReferral("ABC12345", "candidate@example.com")).ReturnsAsync(false);
        fabric.Setup(x => x.ReferredValidation("3001234567", "candidate@example.com"))
            .ReturnsAsync(Response<bool>.SuccessResponse(true, HttpStatusCode.OK));

        var handler = new GetValidateReferredRequestHandler(fabric.Object, referrals.Object);

        var response = await handler.Handle(
            new GetValidateReferredRequest("3001234567", "candidate@example.com", "ABC12345"),
            CancellationToken.None);

        response.Success.ShouldBeTrue();
        response.Data!.Validation.ShouldBeTrue();
        referrals.Verify(x => x.ExistsCandidateReferral("ABC12345", "candidate@example.com"), Times.Once);
        fabric.Verify(x => x.ReferredValidation("3001234567", "candidate@example.com"), Times.Once);
    }

    [Fact]
    public async Task GetValidateReferred_ForDuplicateCandidate_ReturnsConflictBeforeCallingFabric()
    {
        var fabric = new Mock<IFabricService>(MockBehavior.Strict);
        var referrals = new Mock<IReferralRepository>(MockBehavior.Strict);
        referrals.Setup(x => x.ExistsCandidateReferral("68141611", "candidate@example.com")).ReturnsAsync(true);

        var handler = new GetValidateReferredRequestHandler(fabric.Object, referrals.Object);

        var response = await handler.Handle(
            new GetValidateReferredRequest("3001234567", "candidate@example.com", "68141611"),
            CancellationToken.None);

        response.Success.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Errors!.ShouldContain("This candidate has already been referred.");
        fabric.Verify(x => x.ReferredValidation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReferralRepository_DetectsDuplicateCandidateByEmailAndReferralIdCombination()
    {
        var options = new DbContextOptionsBuilder<SolvoRefAppContext>()
            .UseInMemoryDatabase($"referrals-{Guid.NewGuid():N}")
            .Options;

        await using (var setupContext = new SolvoRefAppContext(options))
        {
            var referrer = new ApplicationUser
            {
                Id = "user-1",
                UserName = "user@example.com",
                Email = "user@example.com",
                FullName = "User"
            };

            setupContext.Users.Add(referrer);
            setupContext.Referral.Add(new Referral
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "candidate@example.com",
                CountryCode = "57",
                Phone = "3001234567",
                Area = "Sales",
                ReferralID = "ABC123",
                Experience = "Senior",
                EnglishLevel = "B2",
                Country = "Colombia",
                Account = "Account",
                Comments = "Candidate",
                ReferrerID = referrer.Id,
                ReferralSubmissionKey = ReferralDuplicateKey.Create(referrer.Id, "ABC123", "candidate@example.com"),
                Referrer = referrer
            });
            await setupContext.SaveChangesAsync();
        }

        await using var assertContext = new SolvoRefAppContext(options);
        var repository = new ReferralRepository(assertContext);

        (await repository.ExistsCandidateReferral(" abc123 ", " CANDIDATE@example.com ")).ShouldBeTrue();
        (await repository.ExistsCandidateReferral("abc123", "other@example.com")).ShouldBeFalse();
    }

    [Fact]
    public async Task ReferralRepository_IncludesExpiredReferralsForHiredReconciliation()
    {
        var options = new DbContextOptionsBuilder<SolvoRefAppContext>()
            .UseInMemoryDatabase($"referrals-status-{Guid.NewGuid():N}")
            .Options;

        await using (var setupContext = new SolvoRefAppContext(options))
        {
            var referrer = new ApplicationUser
            {
                Id = "user-1",
                UserName = "user@example.com",
                Email = "user@example.com",
                FullName = "User"
            };

            setupContext.Users.Add(referrer);
            setupContext.Referral.AddRange(
                CreateReferral(referrer, "open@example.com", "OPEN1", "Seeking a position", updatable: true),
                CreateReferral(referrer, "expired@example.com", "EXP1", "Referral expired", updatable: false),
                CreateReferral(referrer, "hired@example.com", "HIR1", "Hired", updatable: false));
            await setupContext.SaveChangesAsync();
        }

        await using var assertContext = new SolvoRefAppContext(options);
        var repository = new ReferralRepository(assertContext);

        var referrals = await repository.GetReferralsOpen();

        referrals.Select(x => x.Email).ShouldBe(["open@example.com", "expired@example.com"], ignoreOrder: true);
    }

    [Fact]
    public void ReferralCompanyResolver_AssignsTbpoAccountListToTransparent()
    {
        const string tbpoAccountLabel = "TBPO CR and SALES roles (Cyracom, Uly, TPG - Travel Pass, Propio, Nolan, Netsol, JLR, Truly, Urgently EHI UDA, Spirit, Honk, TTC)";

        ReferralCompanyResolver.ResolveDataSourcingCompany("Uly", "BackOffice", "United States", "Miami")
            .ShouldBe(ReferralCompanyResolver.Transparent);

        ReferralCompanyResolver.ResolveDataSourcingCompany("The Ticket Clinic", "BackOffice", "United States", "Miami")
            .ShouldBe(ReferralCompanyResolver.Transparent);

        ReferralCompanyResolver.ResolveDataSourcingCompany(tbpoAccountLabel, "BackOffice", "United States", "Miami")
            .ShouldBe(ReferralCompanyResolver.Transparent);
    }

    [Fact]
    public void CreateReferralDto_AllowsCurrentTbpoAccountLabelLength()
    {
        var dto = new CreateReferralDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            CountryCode = "57",
            Phone = "3001234567",
            Area = "Sales",
            ReferralID = "123456",
            Experience = "Junior",
            EnglishLevel = "C1-Advanced",
            Country = "Colombia",
            City = "Medellin",
            Account = "TBPO CR and SALES roles (Cyracom, Uly, TPG - Travel Pass, Propio, Nolan, Netsol, JLR, Truly, Urgently EHI UDA, Spirit, Honk, TTC)",
            HowHear = "Social media",
            Comments = "No"
        };

        Validator.TryValidateObject(dto, new ValidationContext(dto), [], validateAllProperties: true)
            .ShouldBeTrue();
    }

    [Fact]
    public void ReferralCompanyResolver_AssignsSpecificNonTbpoAccountsToSolvo()
    {
        ReferralCompanyResolver.ResolveDataSourcingCompany("Vensure", "Sales", "Colombia", "Bogota")
            .ShouldBe(ReferralCompanyResolver.Solvo);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Staff", "Customer Service", "Mexico", "Merida")
            .ShouldBe(ReferralCompanyResolver.Solvo);
    }

    [Fact]
    public void ReferralCompanyResolver_UsesFrontProfileAndTbpoLocationWhenNoSpecificAccountExists()
    {
        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Sales", "Colombia", "Medellin")
            .ShouldBe(ReferralCompanyResolver.Solvo);

        ReferralCompanyResolver.ResolveDataSourcingCompany("I'm not referring to any particular account", "Customer Service", "Argentina", "Cordoba")
            .ShouldBe(ReferralCompanyResolver.Transparent);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Customer Service", "Mexico", "Chihuahua")
            .ShouldBe(ReferralCompanyResolver.Transparent);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Customer Service", "Colombia", "Barranquilla")
            .ShouldBe(ReferralCompanyResolver.Transparent);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Sales", "Colombia", "Bogota")
            .ShouldBe(ReferralCompanyResolver.Solvo);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Sales", "Colombia", "Cali")
            .ShouldBe(ReferralCompanyResolver.Solvo);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Customer Service", "Argentina", "Buenos Aires CABA")
            .ShouldBe(ReferralCompanyResolver.Solvo);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "Customer Service", "Mexico", "Merida")
            .ShouldBe(ReferralCompanyResolver.Solvo);

        ReferralCompanyResolver.ResolveDataSourcingCompany("Other", "BackOffice", "Colombia", "Bogota")
            .ShouldBe(ReferralCompanyResolver.Solvo);
    }

    [Fact]
    public void DataSourcingColumnMap_AddsCuentaReferidosForSolvoPartnerReferral()
    {
        var values = BuildDataSourcingColumnValueMap(new DataSourcingTable
        {
            ReferrerSolvoPartnerStatus = "Active",
            ReferralFromSolvoPartner = "Yes"
        });

        values["Cuenta_Referidos"].ShouldBe("Solvo Partners ");
        values["SolvoPartner"].ShouldBe("Active");
        values["ReferrerSolvoPartnerStatus"].ShouldBe("Active");
        values["ReferralFromSolvoPartner"].ShouldBe("Yes");
        values["IsSolvoPartnerReferral"].ShouldBe("Yes");
    }

    [Fact]
    public void DataSourcingColumnMap_OmitsCuentaReferidosForNonSolvoPartnerReferral()
    {
        var values = BuildDataSourcingColumnValueMap(new DataSourcingTable
        {
            ReferrerSolvoPartnerStatus = "Inactive",
            ReferralFromSolvoPartner = "No"
        });

        values.ContainsKey("Cuenta_Referidos").ShouldBeFalse();
        values["SolvoPartner"].ShouldBe("Inactive");
        values["ReferrerSolvoPartnerStatus"].ShouldBe("Inactive");
        values["ReferralFromSolvoPartner"].ShouldBe("No");
        values["IsSolvoPartnerReferral"].ShouldBe("No");
    }

    [Fact]
    public void TransparentStatusResolver_RestrictsFirstContactToInitialTwoDayWindow()
    {
        ResolveTransparentStatus(
            new UpdateReferralStatusDto
            {
                ApplicantStatus = "Pending match with a Vacancy - talent pool",
                Ownership = "Owner"
            },
            DateTime.Now.Date.AddDays(-1))
            .ShouldBe("Seeking a position");

        ResolveTransparentStatus(
            new UpdateReferralStatusDto
            {
                ApplicantStatus = string.Empty,
                Ownership = "Owner"
            },
            DateTime.Now.Date.AddDays(-1))
            .ShouldBe("First Contact");

        ResolveTransparentStatus(
            new UpdateReferralStatusDto
            {
                ApplicantStatus = string.Empty,
                Ownership = "Owner"
            },
            DateTime.Now.Date.AddDays(-10))
            .ShouldBeEmpty();

        ResolveTransparentStatus(
            new UpdateReferralStatusDto
            {
                ApplicantStatus = "Pending match with a Vacancy - talent pool",
                Ownership = string.Empty
            },
            DateTime.Now.Date.AddDays(-10))
            .ShouldBe("Seeking a position");

        ResolveTransparentStatus(
            new UpdateReferralStatusDto
            {
                ApplicantStatus = "Pending match with Vacancy - Talent Pool",
                Ownership = string.Empty
            },
            DateTime.Now.Date.AddDays(-10))
            .ShouldBe("Seeking a position");
    }

    [Fact]
    public void SolvoStatusResolver_TreatsResumeAvailableYesAsSeekingAfterCreationDate()
    {
        ResolveSolvoStatus(
            new UpdateReferralStatusDto { ResumeAvailable = "Yes" },
            DateTime.Now,
            "In Progress",
            existsInHunty: true)
            .ShouldBe("Seeking a position");

        ResolveSolvoStatus(
            new UpdateReferralStatusDto { ResumeAvailable = " YES " },
            DateTime.Now.Date.AddDays(-28),
            "In Progress",
            existsInHunty: false)
            .ShouldBe("Seeking a position");
    }

    [Fact]
    public void SolvoStatusResolver_TreatsEmptyResumeAvailableAsNoWhenApplicantExists()
    {
        ResolveSolvoStatus(
            new UpdateReferralStatusDto { ResumeAvailable = string.Empty },
            DateTime.Now.Date.AddDays(-5),
            "First Contact",
            existsInHunty: true)
            .ShouldBe("No Call No Show (NCNS)");

        ResolveSolvoStatus(
            new UpdateReferralStatusDto { ResumeAvailable = " " },
            DateTime.Now.Date.AddDays(-14),
            "No Call No Show (NCNS)",
            existsInHunty: true)
            .ShouldBe("Referral expired");
    }

    [Fact]
    public void SolvoStatusResolver_ExpiresOnlyNoCallNoShowFlow()
    {
        ResolveSolvoStatus(
            new UpdateReferralStatusDto { ResumeAvailable = "No" },
            DateTime.Now.Date.AddDays(-14),
            "In Progress",
            existsInHunty: false)
            .ShouldBe("Referral expired");

        ResolveSolvoStatus(
            new UpdateReferralStatusDto { ResumeAvailable = "Yes" },
            DateTime.Now.Date.AddDays(-59),
            "On Hold/ Waiting for the right match",
            existsInHunty: false)
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateReferralStatus_ReconcilesExpiredReferralToHiredWhenEmployeeRowExists()
    {
        var referrer = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            Email = "user@example.com",
            FullName = "User",
            Status = "Active",
            Country = "Colombia",
            PayrollCompany = "Solvo",
            PaymentFrequency = "mensual"
        };
        var referral = CreateReferral(
            referrer,
            "candidate@example.com",
            "ABC123",
            "Referral expired",
            updatable: false,
            creationDate: DateTime.Now.Date.AddDays(-20));
        var referrals = new List<Referral> { referral };
        var employeeRows = new List<ExtraUser>
        {
            new()
            {
                PersonalId = "ABC123",
                Status = "Active",
                StartDate = DateTime.Now.Date.AddDays(-10)
            }
        };

        var fabric = new Mock<IFabricService>(MockBehavior.Strict);
        fabric.Setup(x => x.GetReferralStatuses(It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .ReturnsAsync(Response<List<UpdateReferralStatusDto>>.SuccessResponse([], HttpStatusCode.OK));
        fabric.Setup(x => x.GetHuntyEmails(It.IsAny<List<string>>()))
            .ReturnsAsync(Response<List<string>>.SuccessResponse([], HttpStatusCode.OK));
        fabric.Setup(x => x.GetEmployeesByPersonalId(It.IsAny<List<string>>()))
            .ReturnsAsync(Response<List<ExtraUser>>.SuccessResponse(employeeRows, HttpStatusCode.OK));

        var referralRepository = new Mock<IReferralRepository>(MockBehavior.Strict);
        referralRepository.Setup(x => x.GetReferralsOpen()).ReturnsAsync(referrals);
        referralRepository.Setup(x => x.Update(referrals)).Returns(Task.CompletedTask);

        var paymentScheduleRepository = new Mock<IPaymentScheduleRepository>(MockBehavior.Strict);
        paymentScheduleRepository.Setup(x => x.GetAll()).ReturnsAsync([]);

        var handler = new UpdateReferralStatusRequestHandler(
            fabric.Object,
            referralRepository.Object,
            paymentScheduleRepository.Object);

        var response = await handler.Handle(new UpdateReferralStatusRequest(), CancellationToken.None);

        response.Success.ShouldBeTrue();
        referral.Status.ShouldBe("Hired");
        referral.Updatable.ShouldBeTrue();
        referral.StartDate.ShouldBe(employeeRows[0].StartDate);
        referral.PaymentMessage.ShouldContain("has not yet completed 30 active days");
    }

    [Fact]
    public void ApplicantStatusResolver_PrefersResumeAvailableYesWhenEmailHasMultipleRows()
    {
        var selectedStatus = ResolveApplicantStatus(
            [
                new UpdateReferralStatusDto
                {
                    Source = "legacy-source",
                    Email = "candidate@example.com",
                    ResumeAvailable = string.Empty
                },
                new UpdateReferralStatusDto
                {
                    Source = "other-source",
                    Email = " candidate@example.com ",
                    ResumeAvailable = "Yes"
                }
            ],
            "App-123",
            "candidate@example.com");

        selectedStatus.ShouldNotBeNull();
        selectedStatus.ResumeAvailable.ShouldBe("Yes");
    }

    [Fact]
    public void ApplicantStatusResolver_IgnoresSameSourceRowsForDifferentEmails()
    {
        var selectedStatus = ResolveApplicantStatus(
            [
                new UpdateReferralStatusDto
                {
                    Source = "App-123",
                    Email = "other-candidate@example.com",
                    ResumeAvailable = "Yes"
                },
                new UpdateReferralStatusDto
                {
                    Source = "App-123",
                    Email = "candidate@example.com",
                    ResumeAvailable = string.Empty
                }
            ],
            "App-123",
            "candidate@example.com");

        selectedStatus.ShouldNotBeNull();
        selectedStatus.Email.ShouldBe("candidate@example.com");
        selectedStatus.ResumeAvailable.ShouldBeEmpty();
    }

    [Fact]
    public void TransparentReferralExpired_UsesInclusiveSixtyDayWindow()
    {
        IsReferralExpired(DateTime.Now.Date.AddDays(-58), isTransparent: true).ShouldBeFalse();
        IsReferralExpired(DateTime.Now.Date.AddDays(-59), isTransparent: true).ShouldBeTrue();
    }

    [Fact]
    public void QuincenalPaymentDate_UsesThePaymentDatePairedWithTheFirstAvailableDeadline()
    {
        var rows = CreateMexicoQuincenalPaymentSchedule();

        ResolveQuincenalPaymentDate(new DateTime(2026, 2, 19), rows)
            .ShouldBe(new DateTime(2026, 2, 27));

        ResolveQuincenalPaymentDate(new DateTime(2026, 2, 27), rows)
            .ShouldBe(new DateTime(2026, 3, 14));
    }

    [Fact]
    public void QuincenalPaymentDate_IncludesExactDeadlineDates()
    {
        var rows = CreateMexicoQuincenalPaymentSchedule();

        ResolveQuincenalPaymentDate(new DateTime(2026, 2, 7), rows)
            .ShouldBe(new DateTime(2026, 2, 14));

        ResolveQuincenalPaymentDate(new DateTime(2026, 2, 22), rows)
            .ShouldBe(new DateTime(2026, 2, 27));
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SolvoRefApp.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Core")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Api")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string ResolveTransparentStatus(UpdateReferralStatusDto updateData, DateTime creationDate)
    {
        var method = typeof(UpdateReferralStatusRequestHandler).GetMethod(
            "ResolveTransparentReferralStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        return (string)method.Invoke(null, [updateData, creationDate])!;
    }

    private static string ResolveSolvoStatus(UpdateReferralStatusDto updateData, DateTime creationDate, string currentStatus, bool existsInHunty)
    {
        var method = typeof(UpdateReferralStatusRequestHandler).GetMethod(
            "ResolveSolvoReferralStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        return (string)method.Invoke(null, [updateData, creationDate, currentStatus, existsInHunty])!;
    }

    private static UpdateReferralStatusDto? ResolveApplicantStatus(
        List<UpdateReferralStatusDto> applicantStatuses,
        string normalizedExpectedSource,
        string normalizedEmail)
    {
        var method = typeof(UpdateReferralStatusRequestHandler).GetMethod(
            "ResolveApplicantStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        return (UpdateReferralStatusDto?)method.Invoke(null, [applicantStatuses, normalizedExpectedSource, normalizedEmail]);
    }

    private static bool IsReferralExpired(DateTime creationDate, bool isTransparent)
    {
        var method = typeof(UpdateReferralStatusRequestHandler).GetMethod(
            "IsReferralExpired",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        return (bool)method.Invoke(null, [creationDate, isTransparent])!;
    }

    private static Dictionary<string, object> BuildDataSourcingColumnValueMap(DataSourcingTable data)
    {
        var method = typeof(DataSourcingService).GetMethod(
            "BuildColumnValueMap",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        return (Dictionary<string, object>)method.Invoke(null, [data])!;
    }

    private static DateTime? ResolveQuincenalPaymentDate(DateTime applicationDate, List<PaymentSchedule> rows)
    {
        var method = typeof(UpdateReferralStatusRequestHandler).GetMethod(
            "ResolveQuincenal",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        return (DateTime?)method.Invoke(null, [applicationDate, rows]);
    }

    private static List<PaymentSchedule> CreateMexicoQuincenalPaymentSchedule()
    {
        return
        [
            new PaymentSchedule
            {
                Employer = "Mexico",
                PaymentFrequency = "QUINCENAL",
                DeadLine1 = new DateTime(2026, 2, 7),
                PaymentDate1 = new DateTime(2026, 2, 14),
                DeadLine2 = new DateTime(2026, 2, 22),
                PaymentDate2 = new DateTime(2026, 2, 27)
            },
            new PaymentSchedule
            {
                Employer = "Mexico",
                PaymentFrequency = "QUINCENAL",
                DeadLine1 = new DateTime(2026, 3, 7),
                PaymentDate1 = new DateTime(2026, 3, 14),
                DeadLine2 = new DateTime(2026, 3, 22),
                PaymentDate2 = new DateTime(2026, 3, 29)
            }
        ];
    }

    private static Referral CreateReferral(
        ApplicationUser referrer,
        string email,
        string referralId,
        string status,
        bool updatable,
        DateTime? creationDate = null)
    {
        return new Referral
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = email,
            CountryCode = "57",
            Phone = "3001234567",
            Area = "Sales",
            ReferralID = referralId,
            Experience = "Senior",
            EnglishLevel = "B2",
            Country = "Colombia",
            Account = "Account",
            Comments = "Candidate",
            Status = status,
            Updatable = updatable,
            ReferrerID = referrer.Id,
            ReferralSubmissionKey = ReferralDuplicateKey.Create(referrer.Id, referralId, email),
            Referrer = referrer,
            CreationDate = creationDate ?? DateTime.Now.Date
        };
    }
}
