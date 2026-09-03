import { IoMdClose } from 'react-icons/io';
import './alertMessage.css';
const AlertMessage = ({ icon, message, onClose, extraClass = '' }) => {
    return (
        <div className="alertMessageContainer">
            <div className={`alertMessage ${extraClass}`}>
                <span>{icon}</span>
                <p>{message}</p>
                <IoMdClose
                    className="alertCloseIcon"
                    onClick={onClose}
                    size={20}
                />
            </div>
        </div>
    );
};

export default AlertMessage;
