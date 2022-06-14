import { BrowserRouter, Routes, Route } from 'react-router-dom';

import Register from './register/Register';

const App = () => {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Register />} />
            </Routes>
        </BrowserRouter>
    )
}

export default App;