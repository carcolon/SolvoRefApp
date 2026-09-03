import './ModalDetails.css';
import recurso23 from '../../assets/images/Recurso 23.png';
import { IoMdClose } from 'react-icons/io';
import puños from '../../assets/images/Recurso 5.png';
import reloj from '../../assets/images/icons8-reloj-48.png';
import telefono from '../../assets/images/icons8-teléfono-desconectado-67.png';
import burbuja from '../../assets/images/icons8-burbuja-de-diálogo-50.png';
import X2 from '../../assets/images/icons8-circulado-x-50.png';
import { Tooltip } from 'react-tooltip';
function ModalDetails({ isOpen, onClose, referral }) {
    if (!isOpen || !referral) {
        return null; // No renderiza nada si el modal no está abierto o no hay referido
    }
    function getSatusColor(status) {
        switch (status) {
            case 'In Progress':
                return '#007bff';

            case 'First Contact':
                return '#007bff';

            case 'Interview':
                return '#007bff';

            case 'No Response':
                return ' #FF0000';

            case 'No Call No Show (NCNS)':
                return ' #FF0000';

            case 'Referral expired':
                return ' #FF0000';

            case 'Rejected':
                return ' #FF0000';

            case 'Hired':
                return '#32CD32';
            default:
                break;
        }
    }

    function getSatusColorFondo(status, itemstatus) {
        if (itemstatus === status) {
            switch (status) {
                case 'Hired':
                    return 'fondo-hired';

                case 'In Progress':
                    return 'fondo-inprogress';

                case 'Rejected':
                    return 'fondo-rejected';

                case 'First Contact':
                    return 'fondo-firstcontact';

                case 'Interview':
                    return 'fondo-interview';

                case 'No Response':
                    return 'fondo-rejected';

                case 'No Call No Show (NCNS)':
                    return 'fondo-rejected';

                case 'Referral expired':
                    return 'fondo-rejected';

                default:
                    return '';
            }
        }
        return '';
    }

    const inProgressTooltip =
        referral.status === 'In Progress'
            ? {
                  'data-tooltip-id': 'tooltip-in-progress',
                  'data-tooltip-content': 'July 20, 2025',
                  'data-tooltip-place': 'bottom',
              }
            : {};

    const firstContactTooltip =
        referral.status === 'First Contact'
            ? {
                  'data-tooltip-id': 'tooltip-first-contact',
                  'data-tooltip-content': 'July 20, 2025',
                  'data-tooltip-place': 'bottom',
              }
            : {};

    const interviewTooltip =
        referral.status === 'Interview'
            ? {
                  'data-tooltip-id': 'tooltip-interview',
                  'data-tooltip-content': 'July 20, 2025',
                  'data-tooltip-place': 'bottom',
              }
            : {};

    const noResponseTooltip =
        referral.status === 'No Response' ||
        referral.status === 'No Call No Show (NCNS)' ||
        referral.status === 'Referral expired'
            ? {
                  'data-tooltip-id': 'tooltip-no-response',
                  'data-tooltip-content': 'July 20, 2025',
                  'data-tooltip-place': 'bottom',
              }
            : {};

    const rejectedTooltip =
        referral.status === 'Rejected'
            ? {
                  'data-tooltip-id': 'tooltip-rejected',
                  'data-tooltip-content': 'July 20, 2025',
                  'data-tooltip-place': 'bottom',
              }
            : {};

    const hiredTooltip =
        referral.status === 'Hired'
            ? {
                  'data-tooltip-id': 'tooltip-hired',
                  'data-tooltip-content': 'July 20, 2025',
                  'data-tooltip-place': 'bottom',
              }
            : {};

    return (
        <div className="recurso53">
            <div className="referral-modal-backdrop" onClick={onClose}>
                <div
                    className="referral-modal-content"
                    onClick={(e) => e.stopPropagation()}
                >
                    <div className="contenido">
                        <IoMdClose
                            className="referral-modal-close-button"
                            onClick={onClose}
                            size={20}
                        />
                        <h2 className="your">Your Referral Name is:</h2>
                        <h3
                            className="nombre"
                            style={{ color: 'rgb(220, 112, 56)' }}
                        >
                            {referral.name}
                        </h3>
                        <img className="recurso23" src={recurso23} alt=""></img>
                        <p className="emaill">
                            <span>Email</span> <br />
                            {referral.email}
                        </p>
                        <p className="referalidd">
                            <span>
                                Document National Identification Number Referral
                            </span>{' '}
                            <br />
                            {referral.referralID}
                        </p>
                        <p className="countryy">
                            <span>Country</span>
                            <br />
                            {referral.country}
                        </p>
                        <p className="currentStatus">
                            <span className="strongs">
                                {' '}
                                You Referral Status is:
                            </span>
                            <p
                                style={{
                                    color: getSatusColor(referral.status),
                                    fontSize: '15px',
                                }}
                                className="statusss"
                            >
                                {referral.status} - {referral?.statusLead}
                            </p>
                        </p>
                        <p className="areaa">
                            <span>Area to apply</span> <br /> {referral.area}
                        </p>
                        <p style={{}} className="phonee">
                            <span>Phone</span>
                            <br />
                            {referral.phone}
                        </p>

                        <p className="cityy">
                            <span>City</span>
                            <br />
                            {referral.city}
                        </p>

                        <div className="estados-inferiores-wrapper">
                            <div
                                className={`estado-item ${getSatusColorFondo(
                                    'In Progress',
                                    referral.status,
                                )}`}
                                {...inProgressTooltip}
                            >
                                <img
                                    className="icono-estado"
                                    src={reloj}
                                    alt="In Progress"
                                ></img>
                                <p>In Progress</p>
                            </div>

                            <div
                                className={`estado-item ${getSatusColorFondo(
                                    'First Contact',
                                    referral.status,
                                )}`}
                                {...firstContactTooltip}
                            >
                                <img
                                    className="icono-estado"
                                    src={telefono}
                                    alt="First Contact"
                                ></img>
                                <p>First Contact</p>
                            </div>

                            <div
                                className={`estado-item ${getSatusColorFondo(
                                    'Interview',
                                    referral.status,
                                )}`}
                                {...interviewTooltip}
                            >
                                <img
                                    className="icono-estado"
                                    src={burbuja}
                                    alt="Interview"
                                ></img>
                                <p>Interview</p>
                            </div>
                            {referral.status === 'No Response' ||
                            referral.status === 'No Call No Show (NCNS)' ||
                            referral.status === 'Referral expired' ? (
                                <div
                                    className={`estado-item ${getSatusColorFondo(
                                        referral.status,
                                        referral.status,
                                    )}`}
                                    {...noResponseTooltip}
                                >
                                    <img
                                        className="icono-estado"
                                        alt={referral.status}
                                        src={X2}
                                    />
                                    <p>{referral.status}</p>
                                </div>
                            ) : referral.status === 'Rejected' ? (
                                <div
                                    className={`estado-item ${getSatusColorFondo(
                                        'Rejected',
                                        referral.status,
                                    )}`}
                                    {...rejectedTooltip}
                                >
                                    <img
                                        className="icono-estado"
                                        alt="Rejected"
                                        src={X2}
                                    />
                                    <p>Rejected</p>
                                </div>
                            ) : (
                                <div
                                    className={`estado-item ${getSatusColorFondo(
                                        'Hired',
                                        referral.status,
                                    )}`}
                                    {...hiredTooltip}
                                >
                                    <img
                                        style={{ width: '90px' }}
                                        src={puños}
                                        alt="Hired"
                                        className="icono-estado"
                                    />
                                    <p>Hired</p>
                                </div>
                            )}

                            {referral.status === 'In Progress' && (
                                <Tooltip id="tooltip-in-progress" />
                            )}
                            {referral.status === 'First Contact' && (
                                <Tooltip id="tooltip-first-contact" />
                            )}
                            {referral.status === 'Interview' && (
                                <Tooltip id="tooltip-interview" />
                            )}
                            {(referral.status === 'No Response' ||
                                referral.status ===
                                    'No Call No Show (NCNS)' ||
                                referral.status === 'Referral expired') && (
                                <Tooltip id="tooltip-no-response" />
                            )}
                            {referral.status === 'Rejected' && (
                                <Tooltip id="tooltip-rejected" />
                            )}
                            {referral.status === 'Hired' && (
                                <Tooltip id="tooltip-hired" />
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default ModalDetails;
