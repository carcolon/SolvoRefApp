import { useCallback, useEffect, useRef, useState } from 'react';
import { useApi } from '../CustomHook/UseApi';
import flechas from '../../assets/images/flechas.png';
import lofoform from '../../assets/images/referralImg.png';
import './Referidos.css';
import OutlinedSelectInput from '../SearchInput/OutlinedSelectInput';
import { referralApi, fabricApi } from '../../Constants/Constants';
import Spinner from '../Spinner/Spinner';
import { toast } from 'react-toastify';
import AlertMessage from '../AlertMessage/AlertMessage';
import { TbAlertTriangleFilled } from 'react-icons/tb';
import { usePageMotion } from '../../animations/usePageMotion';
import TurnstileWidget from '../TurnstileWidget/TurnstileWidget';

const HEARD_ABOUT_OPTIONS = [
    { value: 'already had the link', text: 'already had the link' },
    { value: 'Email campaign', text: 'Email campaign' },
    { value: 'Social media', text: 'Social media' },
    { value: 'Informational banner', text: 'Informational banner' },
    {
        value: 'Recommendation from someone I know',
        text: 'Recommendation from someone I know',
    },
    { value: 'Social media advertising', text: 'Social media advertising' },
    { value: 'QR code at the office', text: 'QR code at the office' },
    { value: 'On-site activation or event', text: 'On-site activation or event' },
    { value: 'Corporate induction', text: 'Corporate induction' },
    { value: "I'm new soulver", text: "I'm new soulver" },
    { value: 'Other', text: 'Other' },
];

const REFERRAL_TERMS_URL = 'https://onesourcecorp.sharepoint.com/:b:/s/SolvoFC_Marketing/IQDKnoDRDdFxTImSoQ2Mv-QHAXppHbUF79RQ6j8JhdySRmM?e=sYVBdB';

function FormReferidos({ setHandleClose, vacancyId, positionName, vacancyCountry, isPublic = false, referralToken = '' }) {
    const formRef = useRef(null);
    const { request } = useApi();
    const [isLoading, setIsLoading] = useState(false);
    const [referralData, setReferralData] = useState({});
    // Estados para cada campo del formulario
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [email, setEmail] = useState('');
    const [countryCode, setCountryCode] = useState(''); // Estado para el código de país
    const [phone, setPhone] = useState('');
    const [experience, setExperience] = useState('');
    const [selectedAreaRol, setSelectedAreaRol] = useState('');
    const [englishLevel, setEnglishLevel] = useState('');
    const [selectedCountry, setSelectedCountry] = useState('');
    const [selectedCity, setSelectedCity] = useState('');
    const [selectAccountRef, setSelectAccountRef] = useState('');
    const [showCityDropDown, setShowCityDropDown] = useState(false);
    const [termConditionCheck, setTermConditionCheck] = useState(false);
    const [otherAccount, setOtherAccount] = useState('');
    const [otherAccountShow, setOtherAccountShow] = useState(false);
    const [heardAbout, setHeardAbout] = useState('');
    const [heardAboutOther, setHeardAboutOther] = useState('');
    const [showHeardAboutOther, setShowHeardAboutOther] = useState(false);
    const [turnstileToken, setTurnstileToken] = useState('');
    const [turnstileRenderKey, setTurnstileRenderKey] = useState(0);
    // const [countryAll, setCountryAll]=useState([]);
    const [showAllForm, setShowAllForm] = useState(false);
    usePageMotion(formRef, [showAllForm]);
    const [formValidation, setFormValidation] = useState({
        firstName: false,
        lastName: false,
        referralId: false,
        email: false,
        countryCode: false,
        phone: false,
        area: false,
        experience: false,
        englishLevel: false,
        account: isPublic,
        otherAccount: true,
        heardAbout: isPublic,
        heardAboutOther: true,
        country: false,
        city: false,
    });

    const [errorValidationMessage, setErrorValidationMessage] = useState('');

    const disableValidateButton = () => {
        const validateButtonKeys = [
            'firstName',
            'lastName',
            'referralId',
            'email',
            'countryCode',
            'phone',
        ];
        // Devuelve true si TODAS esas keys están en true
        return validateButtonKeys.every((key) => formValidation[key] === true);
    };

    const disableSubmitButton = () => {
        return Object.values(formValidation).every((v) => v === true);
    };

    const handleBlurValidation = (validationName, validationStatus) => {
        setFormValidation((prev) => ({
            ...prev,
            [validationName]: validationStatus,
        }));
    };
    const [referralId, setReferralId] = useState('');

    const [coments, setComents] = useState('');
    const referralApiPrefix = isPublic
        ? `${process.env.REACT_APP_API}/api/${referralApi}/public`
        : `${process.env.REACT_APP_API}/api/${referralApi}`;
    const turnstileSiteKey = process.env.REACT_APP_TURNSTILE_SITE_KEY || '';
    const handleTurnstileExpire = useCallback(() => {
        setTurnstileToken('');
    }, []);

    const getReferralInformation = async () => {
        setIsLoading(true);
        const referralExperience = await request(
            `${referralApiPrefix}/experience/all`,
        );
        const referralEnglishLevel = await request(
            `${referralApiPrefix}/englishlevel/all`,
        );

        const referralAccount = isPublic
            ? null
            : await request(`${referralApiPrefix}/account/all`);

        const referralArea = await request(
            `${referralApiPrefix}/applyarea/all`,
        );

        const referralCountry = await request(
            `${referralApiPrefix}/country/all`,
        );

        const referralPhoneCode = await request(
            `${referralApiPrefix}/phonecode/all`,
        );

        if (referralPhoneCode != null) {
            setReferralData((prev) => ({
                ...prev,
                phoneCodeData: referralPhoneCode,
            }));
        }

        if (referralAccount != null) {
            setReferralData((prev) => ({
                ...prev,
                accountData: referralAccount,
            }));
        }
        if (referralCountry != null) {
            setReferralData((prev) => ({
                ...prev,
                countryData: referralCountry,
            }));
        }
        if (referralExperience != null) {
            setReferralData((prev) => ({
                ...prev,
                experienceData: referralExperience,
            }));
        }
        if (referralArea != null) {
            setReferralData((prev) => ({
                ...prev,
                referralAreaData: referralArea,
            }));
        }
        if (referralEnglishLevel != null) {
            setReferralData((prev) => ({
                ...prev,
                englishLevelData: referralEnglishLevel,
            }));
        }
        setIsLoading(false);
    };

    const handleChangeAccount = (value) => {
        const otherValidation = value.toLowerCase() === 'other';
        setSelectAccountRef(value);
        setOtherAccountShow(otherValidation);
        setOtherAccount('');
        if (otherValidation) {
            handleBlurValidation('otherAccount', true);
        }
    };

    const handleCountrySelection = async (value) => {
        setSelectedCountry(value);
        const referralCity = await request(
            `${referralApiPrefix}/city/all/${value}`,
        );
        if (referralCity != null) {
            setSelectedCity('');
            setReferralData((prev) => ({
                ...prev,
                cityData: referralCity,
            }));
            setShowCityDropDown(false);
            if (referralCity.length === 0) {
                handleBlurValidation('city', true);
            }
            setShowCityDropDown(referralCity.length > 0);
        }
    };

    const handleHeardAboutSelection = (value) => {
        const requiresOther = value.toLowerCase() === 'other';
        setHeardAbout(value);
        setShowHeardAboutOther(requiresOther);
        setHeardAboutOther('');
        handleBlurValidation('heardAbout', !!value);
        handleBlurValidation('heardAboutOther', !requiresOther);
    };

    const externalValidateEmail = ({ value }) => {
        const text = value.trim();
        // Regex simple: letras, números, puntos, guiones, guion bajo; dominio básico con TLD de 2+ letras.
        const emailRegex = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

        if (!emailRegex.test(text)) {
            return { valid: false, error: 'Format invalid' };
        }
        return true; // válido
    };

    const externalValidatePhone = ({ value }) => {
        const text = value.trim();
        // Regex simple: letras, números, puntos, guiones, guion bajo; dominio básico con TLD de 2+ letras.
        const phoneRegex = /^\d+$/;

        if (!phoneRegex.test(text)) {
            return { valid: false, error: 'Only numbers allow' };
        }
        return true; // válido
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        const account = isPublic
            ? 'Public referral link'
            : selectAccountRef.toLowerCase() === 'other'
                ? `${selectAccountRef} - ${otherAccount.trim()}`
                : selectAccountRef;
        const howHear = isPublic
            ? 'Referral link'
            : heardAbout.toLowerCase() === 'other'
                ? `${heardAbout} - ${heardAboutOther.trim()}`
                : heardAbout;
        const referralPayload = {
            FirstName: firstName.trim(),
            LastName: lastName.trim(),
            Email: email.trim(),
            CountryCode: countryCode,
            Phone: phone.trim(),
            Area: selectedAreaRol,
            ReferralID: referralId.trim(),
            VacancyId: '',
            ExternalVacancyId: vacancyId || '',
            Position: positionName || '',
            VacancyCountry: vacancyCountry || '',
            Experience: experience,
            Country: selectedCountry,
            City: selectedCity,
            Account: account,
            HowHear: howHear,
            Comments: coments,
            EnglishLevel: englishLevel,
        };
        const formData = new FormData();
        Object.entries(referralPayload).forEach(([key, value]) => {
            formData.append(key, value);
        });

        if (isPublic && !turnstileToken) {
            toast.error('Please complete the captcha validation.');
            setIsLoading(false);
            return;
        }

        const response = await request(
            isPublic
                ? `${process.env.REACT_APP_API}/api/${referralApi}/public/${referralToken}`
                : `${process.env.REACT_APP_API}/api/${referralApi}`,
            {
                method: 'POST',
                body: isPublic
                    ? {
                        referral: referralPayload,
                        turnstileToken,
                    }
                    : formData,
            },
        );
        if (response != null) {
            setHandleClose(false);
            toast.success(response);
        } else if (isPublic) {
            setTurnstileToken('');
            setTurnstileRenderKey((prev) => prev + 1);
        }
        setIsLoading(false);
    };
    const handleCloseAlert = () => {
        setErrorValidationMessage('');
    };

    const handleValidateButton = async () => {
        setIsLoading(true);
        const referredValidation = await request(
            isPublic
                ? `${process.env.REACT_APP_API}/api/${referralApi}/public/${referralToken}/validate/referred`
                : `${process.env.REACT_APP_API}/api/${fabricApi}/validate/referred`,
            {
                method: 'POST',
                body: {
                    phone,
                    email,
                    referralId: referralId.trim(),
                },
            },
        );
        setIsLoading(false);
        if (referredValidation != null) {
            if (referredValidation.validation) {
                setShowAllForm(true);
            } else {
                setErrorValidationMessage(referredValidation.message);
            }
        }
    };

    useEffect(() => {
        getReferralInformation();
    }, []);

    useEffect(() => {
        setShowAllForm(false);
    }, [email, phone]);

    return (
        <>
            <Spinner isLoading={isLoading} />
            <div ref={formRef}>
            <div className="form-header" data-hero>
                <div className="flecha-titulo">
                    <h2>{isPublic ? 'Submit your application' : 'Create New Referral'}</h2>
                    <img className="flechass" src={flechas} alt=""></img>
                </div>
                <img src={lofoform} alt="logo del formulario" data-float></img>
            </div>

            <form
                onSubmit={handleSubmit}
                className="referral-form-grid"
                noValidate
            >
                <div className="form-right-panel">
                    <div className="form-row" data-reveal>
                        <OutlinedSelectInput
                            showToastOnError={false}
                            id="firstName"
                            type="text"
                            label="First Name"
                            placeholder="First Name"
                            value={firstName}
                            onChange={(e) => {
                                setFirstName(e.target.value);
                            }}
                            required
                            defaultInvalid={true} // ⬅ inicia como inválido
                            validateOnChange={true} // ⬅ valida en cada cambio
                            debounceDelay={400} // ⬅ debounce
                            onValidateChange={(ok) => {
                                handleBlurValidation('firstName', ok);
                            }}
                        />

                        <div className="outlined-input-container">
                            <OutlinedSelectInput
                                showToastOnError={false}
                                id="lastName"
                                type="text"
                                label="Last Name"
                                placeholder="Last Name"
                                value={lastName}
                                onChange={(e) => {
                                    setLastName(e.target.value);
                                }}
                                required
                                defaultInvalid={true} // ⬅ inicia como inválido
                                validateOnChange={true} // ⬅ valida en cada cambio
                                debounceDelay={400} // ⬅ debounce
                                onValidateChange={(ok) => {
                                    handleBlurValidation('lastName', ok);
                                }}
                            />
                        </div>
                    </div>
                    <div className="form-row" data-reveal>
                        <div className="outlined-input-container">
                            <OutlinedSelectInput
                                showToastOnError={false}
                                id="referralId"
                                type="text"
                                label="Document National Identification Number Referral"
                                placeholder="DNI"
                                value={referralId}
                                onChange={(e) => {
                                    setReferralId(e.target.value);
                                }}
                                required
                                defaultInvalid={true} // ⬅ inicia como inválido
                                validateOnChange={true} // ⬅ valida en cada cambio
                                debounceDelay={400} // ⬅ debounce
                                onValidateChange={(ok) => {
                                    handleBlurValidation('referralId', ok);
                                }}
                                toolTipText="Hello, it is important to enter your referral’s identification number, as it will be required for processing the program payment"
                            />
                        </div>
                        <div className="outlined-input-container">
                            <OutlinedSelectInput
                                showToastOnError={false}
                                id="email"
                                type="email"
                                label="Email"
                                placeholder="Email"
                                value={email}
                                onChange={(e) => {
                                    setEmail(e.target.value);
                                }}
                                required
                                externalValidate={externalValidateEmail}
                                defaultInvalid={true} // ⬅ inicia como inválido
                                validateOnChange={true} // ⬅ valida en cada cambio
                                debounceDelay={400} // ⬅ debounce
                                onValidateChange={(ok) => {
                                    handleBlurValidation('email', ok);
                                }}
                            />
                        </div>
                    </div>
                    <div className="form-row phone-section" data-reveal>
                        <OutlinedSelectInput
                            showToastOnError={false}
                            id="countryCode"
                            label="Country Code"
                            isSearch
                            required
                            options={referralData.phoneCodeData}
                            onOptionSelect={(opt) => setCountryCode(opt.value)}
                            placeholder="Select country code"
                            variantClassName="select-variant country-code-select"
                            defaultInvalid={true} // ⬅ inicia como inválido
                            validateOnChange={true} // ⬅ valida en cada cambio
                            debounceDelay={400} // ⬅ debounce
                            onValidateChange={(ok) => {
                                handleBlurValidation('countryCode', ok);
                            }}
                        />
                        <OutlinedSelectInput
                            showToastOnError={false}
                            id="phone"
                            type="text"
                            label="Phone"
                            placeholder="e.j 123456789"
                            value={phone}
                            onChange={(e) => {
                                setPhone(e.target.value);
                            }}
                            required
                            externalValidate={externalValidatePhone}
                            defaultInvalid={true} // ⬅ inicia como inválido
                            validateOnChange={true} // ⬅ valida en cada cambio
                            debounceDelay={400} // ⬅ debounce
                            onValidateChange={(ok) => {
                                handleBlurValidation('phone', ok);
                            }}
                        />
                    </div>
                    {errorValidationMessage.length > 0 && (
                        <div className="form-row" data-reveal>
                            <AlertMessage
                                icon={
                                    <TbAlertTriangleFilled
                                        color="rgb(242, 218, 6)"
                                        size={35}
                                    />
                                }
                                message={errorValidationMessage}
                                onClose={handleCloseAlert}
                            />
                        </div>
                    )}

                    {showAllForm && (
                        <>
                            <div className="form-row" data-reveal>
                                <OutlinedSelectInput
                                    showToastOnError={false}
                                    id="area"
                                    label="Area to apply"
                                    isSearch
                                    options={referralData.referralAreaData}
                                    onOptionSelect={(opt) =>
                                        setSelectedAreaRol(opt.value)
                                    }
                                    required
                                    placeholder="Select area"
                                    variantClassName="select-variant country-code-select"
                                    defaultInvalid={true} // ⬅ inicia como inválido
                                    validateOnChange={true} // ⬅ valida en cada cambio
                                    debounceDelay={400} // ⬅ debounce
                                    onValidateChange={(ok) => {
                                        handleBlurValidation('area', ok);
                                    }}
                                />
                            </div>
                            <div className="form-row" data-reveal>
                                <OutlinedSelectInput
                                    showToastOnError={false}
                                    id="experience"
                                    label="Experience Level"
                                    isSearch
                                    required
                                    options={referralData.experienceData}
                                    onOptionSelect={(opt) =>
                                        setExperience(opt.value)
                                    }
                                    placeholder="Select Experience Level"
                                    variantClassName="select-variant country-code-select"
                                    defaultInvalid={true} // ⬅ inicia como inválido
                                    validateOnChange={true} // ⬅ valida en cada cambio
                                    debounceDelay={400} // ⬅ debounce
                                    onValidateChange={(ok) => {
                                        handleBlurValidation('experience', ok);
                                    }}
                                />
                            </div>
                            <div className="form-row" data-reveal>
                                <OutlinedSelectInput
                                    showToastOnError={false}
                                    id="englishLevel"
                                    label="English Level"
                                    isSearch
                                    required
                                    options={referralData.englishLevelData}
                                    onOptionSelect={(opt) =>
                                        setEnglishLevel(opt.value)
                                    }
                                    placeholder="Select English Level"
                                    variantClassName="select-variant country-code-select"
                                    defaultInvalid={true} // ⬅ inicia como inválido
                                    validateOnChange={true} // ⬅ valida en cada cambio
                                    debounceDelay={400} // ⬅ debounce
                                    onValidateChange={(ok) => {
                                        handleBlurValidation(
                                            'englishLevel',
                                            ok,
                                        );
                                    }}
                                />
                            </div>
                            {!isPublic && (
                            <div className="form-row">
                                <OutlinedSelectInput
                                    showToastOnError={false}
                                    id="account"
                                    label="Are you referring someone for a specific account? If so, which one?"
                                    isSearch
                                    options={referralData.accountData}
                                    onOptionSelect={(opt) =>
                                        handleChangeAccount(opt.value)
                                    }
                                    required
                                    placeholder="Select account"
                                    variantClassName="select-variant country-code-select"
                                    defaultInvalid={true} // ⬅ inicia como inválido
                                    validateOnChange={true} // ⬅ valida en cada cambio
                                    debounceDelay={400} // ⬅ debounce
                                    onValidateChange={(ok) => {
                                        handleBlurValidation('account', ok);
                                    }}
                                />
                            </div>
                            )}
                            {!isPublic && otherAccountShow && (
                                <div className="form-row" data-reveal>
                                    <OutlinedSelectInput
                                        showToastOnError={false}
                                        id="otherAccount"
                                        type="text"
                                        label="Other Account"
                                        placeholder="other account"
                                        value={otherAccount}
                                        onChange={(e) => {
                                            setOtherAccount(e.target.value);
                                        }}
                                        required
                                        defaultInvalid={true} // ⬅ inicia como inválido
                                        validateOnChange={true} // ⬅ valida en cada cambio
                                        debounceDelay={400} // ⬅ debounce
                                        onValidateChange={(ok) => {
                                            handleBlurValidation(
                                                'otherAccount',
                                                ok,
                                            );
                                        }}
                                    />
                                </div>
                            )}
                            {!isPublic && (
                            <div className="form-row" data-reveal>
                                <OutlinedSelectInput
                                    showToastOnError={false}
                                    id="heardAbout"
                                    label="How did you hear about our platform?"
                                    isSearch
                                    options={HEARD_ABOUT_OPTIONS}
                                    onOptionSelect={(opt) =>
                                        handleHeardAboutSelection(opt.value)
                                    }
                                    required
                                    placeholder="Select an option"
                                    variantClassName="select-variant country-code-select"
                                    defaultInvalid={true}
                                    validateOnChange={true}
                                    debounceDelay={400}
                                    onValidateChange={(ok) => {
                                        handleBlurValidation('heardAbout', ok);
                                    }}
                                />
                            </div>
                            )}
                            {!isPublic && showHeardAboutOther && (
                                <div className="form-row" data-reveal>
                                    <OutlinedSelectInput
                                        showToastOnError={false}
                                        id="heardAboutOther"
                                        type="text"
                                        label="Tell us which one"
                                        placeholder="Type how you heard about the platform"
                                        value={heardAboutOther}
                                        onChange={(e) => {
                                            setHeardAboutOther(e.target.value);
                                        }}
                                        required
                                        defaultInvalid={true}
                                        validateOnChange={true}
                                        debounceDelay={400}
                                        onValidateChange={(ok) => {
                                            handleBlurValidation('heardAboutOther', ok);
                                        }}
                                    />
                                </div>
                            )}
                            <h3 className="section-title" data-reveal>Other Details</h3>
                            <div className="country form-row" data-reveal>
                                <OutlinedSelectInput
                                    showToastOnError={false}
                                    id="country"
                                    label="Country"
                                    isSearch
                                    options={referralData.countryData}
                                    onOptionSelect={(opt) =>
                                        handleCountrySelection(opt.value)
                                    }
                                    required
                                    placeholder="Select Country"
                                    variantClassName="select-variant country-code-select"
                                    defaultInvalid={true} // ⬅ inicia como inválido
                                    validateOnChange={true} // ⬅ valida en cada cambio
                                    debounceDelay={400} // ⬅ debounce
                                    onValidateChange={(ok) => {
                                        handleBlurValidation('country', ok);
                                    }}
                                />
                                {showCityDropDown && (
                                    <OutlinedSelectInput
                                        showToastOnError={false}
                                        id="city"
                                        label="City"
                                        isSearch
                                        options={referralData.cityData}
                                        onOptionSelect={(opt) =>
                                            setSelectedCity(opt.value)
                                        }
                                        required
                                        placeholder="Select City"
                                        variantClassName="select-variant country-code-select"
                                        defaultInvalid={true} // ⬅ inicia como inválido
                                        validateOnChange={true} // ⬅ valida en cada cambio
                                        debounceDelay={400} // ⬅ debounce
                                        onValidateChange={(ok) => {
                                            handleBlurValidation('city', ok);
                                        }}
                                    />
                                )}
                            </div>

                            <div className="outlined-input-container full-width comments-textarea-wrapper" data-reveal>
                                <textarea
                                    id="coments"
                                    placeholder="Comments"
                                    value={coments}
                                    onChange={(e) => setComents(e.target.value)}
                                    rows="10"
                                ></textarea>
                                <fieldset className="input-outline">
                                    <legend className="input-legend">
                                        <span>Comments*</span>
                                    </legend>
                                </fieldset>
                            </div>
                        </>
                    )}
                </div>
                <div className="form-footer2">
                    {showAllForm && (
                        <>
                            {isPublic && (
                                <>
                                    <div className="public-referral-privacy" role="note">
                                        <div className="privacy-lock" aria-hidden="true">
                                            <svg viewBox="0 0 32 32" focusable="false">
                                                <path d="M10.2 13.8V10.7C10.2 7.3 12.8 4.8 16 4.8C19.2 4.8 21.8 7.3 21.8 10.7V13.8H23.1C24.7 13.8 26 15.1 26 16.7V24.4C26 26 24.7 27.3 23.1 27.3H8.9C7.3 27.3 6 26 6 24.4V16.7C6 15.1 7.3 13.8 8.9 13.8H10.2ZM12.8 13.8H19.2V10.7C19.2 8.8 17.8 7.4 16 7.4C14.2 7.4 12.8 8.8 12.8 10.7V13.8ZM14.8 20.5V23.4H17.2V20.5C18 20.1 18.5 19.3 18.5 18.4C18.5 17 17.4 15.9 16 15.9C14.6 15.9 13.5 17 13.5 18.4C13.5 19.3 14 20.1 14.8 20.5Z" />
                                            </svg>
                                            <span className="privacy-lock-mark privacy-lock-mark-one" />
                                            <span className="privacy-lock-mark privacy-lock-mark-two" />
                                            <span className="privacy-lock-mark privacy-lock-mark-three" />
                                        </div>
                                        <span className="privacy-divider" aria-hidden="true" />
                                        <p>
                                            <span>The information you share will be kept confidential and used only</span>
                                            <strong>to contact you and follow up on your process.</strong>
                                        </p>
                                        <span className="privacy-star privacy-star-large" aria-hidden="true" />
                                        <span className="privacy-star privacy-star-small" aria-hidden="true" />
                                        <span className="privacy-dot" aria-hidden="true" />
                                    </div>
                                    <TurnstileWidget
                                        key={turnstileRenderKey}
                                        siteKey={turnstileSiteKey}
                                        onVerify={setTurnstileToken}
                                        onExpire={handleTurnstileExpire}
                                    />
                                </>
                            )}
                            {!isPublic && (
                            <div className="form-footer-term">
                                <input
                                    value={termConditionCheck}
                                    type="checkbox"
                                    onChange={(e) =>
                                        setTermConditionCheck(e.target.checked)
                                    }
                                />
                                <p>
                                    I have read and accept the{' '}
                                    <a
                                        href={REFERRAL_TERMS_URL}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                    >
                                        terms and conditions
                                    </a>
                                </p>
                            </div>
                            )}
                            <button
                                type="submit"
                                className="btn-submit gsap-button"
                                disabled={
                                    !disableSubmitButton() ||
                                    (!isPublic && !termConditionCheck) ||
                                    (isPublic && !turnstileToken)
                                }
                            >
                                Submit
                            </button>
                        </>
                    )}
                    {!showAllForm && (
                        <button
                            disabled={
                                !disableValidateButton() ||
                                errorValidationMessage.length > 0
                            }
                            type="button"
                            className="btn-submit btn-validate gsap-button"
                            onClick={handleValidateButton}
                        >
                            Validate
                        </button>
                    )}
                </div>
            </form>
            </div>
        </>
    );
}
export default FormReferidos;
