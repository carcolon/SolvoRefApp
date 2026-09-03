import { FiPhoneCall } from 'react-icons/fi';
import './ReferralFooter.css';

const TERMS_URL = 'https://onesourcecorp.sharepoint.com/sites/SolvoFC_Marketing/Shared%20Documents/Forms/AllItems.aspx?id=%2Fsites%2FSolvoFC%5FMarketing%2FShared%20Documents%2FMarketing%20%2D%20Solvo%2FReferidos%20Global%2FTyC%20%2D%20FAQs%20%28Mayo%29%2FT%C3%A9rminos%20y%20condiciones%20%2D%20FAQs&p=true&ct=1787770962277&or=Teams%2DHL&ga=1&LOF=1';
const FAQ_URL = 'https://onesourcecorp.sharepoint.com/sites/SolvoFC_Marketing/Shared%20Documents/Forms/AllItems.aspx?id=%2Fsites%2FSolvoFC%5FMarketing%2FShared%20Documents%2FMarketing%20%2D%20Solvo%2FReferidos%20Global%2FTyC%20%2D%20FAQs%20%28Mayo%29%2FT%C3%A9rminos%20y%20condiciones%20%2D%20FAQs&p=true&ct=1787770962277&or=Teams%2DHL&ga=1&LOF=1';
const WOLFPACK_URL = 'https://teams.microsoft.com/l/app/4c660692-84b1-4f46-9355-8f8b19043a2c?source=app-bar-share-entrypoint';

function ReferralFooter() {
    return (
        <footer className="referral-footer" aria-label="Referral support footer">
            <div className="referral-footer__contact">
                <FiPhoneCall aria-hidden="true" />
                <span>Contact </span>
                <a href={WOLFPACK_URL} target="_blank" rel="noreferrer">
                    Call The Wolfpack
                </a>
                <span>-</span>
                <a href="mailto:referidos@solvoglobal.com">referidos@solvoglobal.com</a>
            </div>
            <div className="referral-footer__links">
                <a href={TERMS_URL} target="_blank" rel="noreferrer">
                    Terms &amp; Conditions
                </a>
                <span aria-hidden="true">|</span>
                <a href={FAQ_URL} target="_blank" rel="noreferrer">
                    FAQs
                </a>
            </div>
        </footer>
    );
}

export default ReferralFooter;
