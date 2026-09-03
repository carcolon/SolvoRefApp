import { Link } from 'react-router-dom';
import './menuItem.css';

const MenuItem = ({
    icon,
    iconAlt,
    text,
    showText,
    to,
    isSelected,
    onHandlerLink,
}) => {
    return (
        <Link
            to={to}
            className={`itemContainer gsap-button ${isSelected ? 'selected' : ''}`}
            onClick={onHandlerLink}
        >
            <img src={icon} alt={iconAlt} />
            {showText && <p>{text}</p>}
        </Link>
    );
};

export default MenuItem;
