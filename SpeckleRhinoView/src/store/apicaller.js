import Axios from 'axios'

export default {
  getStreams( account ) {
    return new Promise( ( resolve, reject ) => {
      if ( !account ) return reject( 'No account provided' )
      Axios.get( account.restApi + '/accounts/streams', { headers: { 'Authorization': account.apiToken } } )
        .then( res => {
          resolve( res.data )
        } )
        .catch( err => {
          reject( err )
        } )
    } )
  },

  updateStreamName( client ) {
    return Axios.put( client.BaseUrl + '/streams/' + client.stream.streamId + '/name', { name: client.stream.name }, { headers:  { 'Authorization': client.apiToken } } )
    // return Axios.post( url, {}, {headersdd} )
  },

  registerAccount( data ) {
    return new Promise( ( resolve, reject ) => {
      Axios.post( data.serverUrl + '/accounts/register', {
          email: data.userEmail,
          password: data.password,
          name: data.userName,
          surname: data.userSurname
        } )
        .then( res => {
          resolve( res )
        } )
        .catch( err => {
          reject( new Error( err.response.data.message ? err.response.data.message : err.message ) )
        } )
    } )
  },

  loginAccount( data ) {
    return Axios.post( data.serverUrl + '/accounts/login', { email: data.userEmail, password: data.password } )
  },

  getServerName( url ) {
    return new Promise( ( resolve, reject ) => {
      Axios.get( url )
        .then( res => {
          resolve( res.data.serverName )
        } )
        .catch( err => {
          reject( err )
        } )
    } )
  }
}