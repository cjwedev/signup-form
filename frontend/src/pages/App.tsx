import { BrowserRouter, Routes, Route } from 'react-router-dom';

import Register from './register/Register';
import Successful from './register/Successful';

const App = () => {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Register />} />
                <Route path="/successful" element={<Successful />} />
            </Routes>
        </BrowserRouter>
    )
}

export default App;