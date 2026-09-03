import './detailForm.css';
import inProgress from '../../assets/images/inprogresStatus.png';
import firstContact from '../../assets/images/firtsContactStatus.png';
import seeking from '../../assets/images/seekingStatus.png';
import hire from '../../assets/images/hireStatus.png';
import ncns from '../../assets/images/ncns-status.svg';
import expired from '../../assets/images/referral-expired-status.svg';
import onHold from '../../assets/images/on-hold-status.svg';
import recurso23 from '../../assets/images/Recurso 23.png';

const BASE_STATUS_STEPS = [
    { key: 'in progress', label: 'In Progress', icon: inProgress, alt: 'in progress' },
    { key: 'first contact', label: 'First Contact', icon: firstContact, alt: 'first contact' },
    { key: 'seeking a position', label: 'Seeking a position', icon: seeking, alt: 'seeking a position' },
    { key: 'hired', label: 'Hired', icon: hire, alt: 'hired', imageClassName: 'hire' },
];

const ON_HOLD_STEP = {
    key: 'on hold/ waiting for the right match',
    label: 'On Hold',
    icon: onHold,
    alt: 'on hold waiting for the right match',
    insertAfter: 'seeking a position',
    imageClassName: 'on-hold-status-icon',
};

const EXCEPTION_STATUS_STEPS = {
    'no call no show (ncns)': {
        key: 'no call no show (ncns)',
        label: 'No Call No Show',
        icon: ncns,
        alt: 'no call no show',
        insertAfter: 'first contact',
        imageClassName: 'exception-status-icon',
    },
    'referral expired': {
        key: 'referral expired',
        label: 'Referral expired',
        icon: expired,
        alt: 'referral expired',
        insertAfter: 'hired',
        imageClassName: 'exception-status-icon exception-status-icon-expired',
    },
};

const STATUS_MESSAGES = {
    'in progress': [
        'Your referral was successfully registered. 🚀',
        'This status will be updated once they complete the first screening with Ana, our virtual assistant.',
        'If 30 days pass without contact, you will be able to refer them again to reactivate the opportunity.',
    ],
    'first contact': [
        'Your referral has already received the first contact from Ana! 👋',
        'This status will be updated according to their progress in the process.',
        'If 30 days pass without contact, you will be able to refer them again for a new opportunity.',
    ],
    'seeking a position': [
        'Your referral passed the first screening, and we are now looking for an opportunity that matches their profile. 🔎',
        'If we find an ideal vacancy, they will receive contact from the recruitment team to continue the process.',
        'If 30 days pass without contact, you will be able to refer them again.',
    ],
    'referral expired': [
        'This opportunity has come to an end because no suitable position was found according to the profile or there was no continuation in the process.',
        'You can refer them again to give them a new opportunity. 🚀',
    ],
    'no call no show (ncns)': [
        'We were unable to contact your referral after several attempts. 📲',
        'You will be able to refer them again after 30 days for a new opportunity.',
    ],
    'on hold/ waiting for the right match': [
        'Your referral is still a good profile, but for now, we have not found the ideal opportunity. 🔎',
        'Remember that they can be referred again. When doing so, make sure to enter their information correctly and that they have their phone available to receive our contact.',
    ],
};

const DetailForm = ({ referralData }) => {
    const status = referralData?.status?.toLowerCase().trim() ?? '';
    const exceptionStep = EXCEPTION_STATUS_STEPS[status];
    const isExceptionStatus = !!exceptionStep;
    const shouldShowOnHoldStep =
        status === ON_HOLD_STEP.key;
    const statusMessage = STATUS_MESSAGES[status];
    const statusSteps = BASE_STATUS_STEPS.reduce((steps, step) => {
        steps.push(step);
        if (
            shouldShowOnHoldStep &&
            ON_HOLD_STEP.insertAfter === step.key
        ) {
            steps.push(ON_HOLD_STEP);
        }
        if (exceptionStep?.insertAfter === step.key) {
            steps.push(exceptionStep);
        }
        return steps;
    }, []);

    return (
        <div className="detailFormContainerMain">
            <div className="detailFormTitleContainer">
                <h5>You Referral Name is:</h5>
                <p>{referralData?.name}</p>
                <img src={recurso23} alt=" firma" />
            </div>
            <div className="detailFormContainer">
                <div className="detailFormDivContainer">
                    <p>Email</p>
                    <p>{referralData?.email}</p>
                </div>
                <div className="detailFormDivContainer">
                    <p>Phone</p>
                    <p>{referralData?.phone}</p>
                </div>
                <div className="detailFormDivContainer">
                    <p>Document National Identification Number Referral</p>
                    <p>{referralData?.referralID}</p>
                </div>
                <div className="detailFormDivContainer">
                    <p>Area To Apply</p>
                    <p>{referralData?.area}</p>
                </div>
                <div className="detailFormDivContainer">
                    <p>Country</p>
                    <p>{referralData?.country}</p>
                </div>

                <div className="detailFormDivContainer">
                    <p>City</p>
                    <p>{referralData?.city}</p>
                </div>
                <div className="span-2">
                    You Referral Status is:{' '}
                    <strong
                        className={`formStatus ${isExceptionStatus ? 'formStatusAlert' : ''}`}
                    >
                        {referralData?.status}{' '}
                        {referralData?.statusLead &&
                            `- ${referralData?.statusLead}`}
                    </strong>
                </div>
                <div className="span-2 detailFormStatusContainer">
                    {statusSteps.map((step) => {
                        const isSelected = status === step.key;
                        const isException =
                            step.key === 'no call no show (ncns)' ||
                            step.key === 'referral expired' ||
                            step.key === ON_HOLD_STEP.key;

                        return (
                            <div
                                key={step.key}
                                className={`detailFormStatusContainerItem ${isSelected ? 'detailFormStatusContainerItemSelected' : ''} ${isSelected && isException ? 'detailFormStatusContainerItemAlert' : ''}`}
                            >
                                <img
                                    src={step.icon}
                                    className={step.imageClassName}
                                    alt={step.alt}
                                />
                                <p>{step.label}</p>
                            </div>
                        );
                    })}
                </div>
                {statusMessage && (
                    <div className="span-2">
                        <div className="detailFormStatusMessage">
                            {statusMessage.map((message) => (
                                <p key={message}>{message}</p>
                            ))}
                        </div>
                    </div>
                )}
                {referralData?.paymentMessage?.length > 0 && (
                    <div className="span-2 ">
                        <h6>Payment information:</h6>
                        <p className="detailFormPaymentMessage">
                            {referralData?.paymentMessage}
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default DetailForm;
