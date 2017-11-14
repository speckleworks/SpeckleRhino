<template>
  <div id="app">
    <v-app dark>
      <v-tabs dark grow>
        <v-toolbar class='grey darken-4 white--text' fixed top>
          <v-toolbar-title>Speckle</v-toolbar-title>
          <v-spacer></v-spacer>
          <v-btn icon @click.native='showDev' class='white--text'>
            <v-icon>code</v-icon>
          </v-btn>
          <v-btn icon @click.native='saveClients' class='white--text'>
            <v-icon>save</v-icon>
          </v-btn>
          <v-btn icon @click.native='readClients' class='white--text'>
            <v-icon>description</v-icon>
          </v-btn>
        </v-toolbar>
        <v-tabs-bar class='grey darken-2 white--text' fixed top style='margin-top:60px;'>
          <v-tabs-item key='Clients' href='Clients'>
            Clients
          </v-tabs-item>
          <v-tabs-item key='accounts' href='accounts'>
            Accounts
          </v-tabs-item>
          <v-tabs-item key='objectInspector' href='objectInspector'>
            Object
          </v-tabs-item>
          <v-tabs-slider color='white'></v-tabs-slider>
        </v-tabs-bar>
        <v-tabs-items>
          <v-tabs-content id='Clients' key='Clients'>
            <v-card flat>
              <client-manager></client-manager>
            </v-card>
          </v-tabs-content>        
          <v-tabs-content id='accounts' key='accounts'>
            <v-card flat>
              <accounts-manager></accounts-manager>
            </v-card>
          </v-tabs-content>        
          <v-tabs-content id='objectInspector' key='objectInspector'>
            <v-card flat>
              <!-- <accounts-manager></accounts-manager> -->
              <h1>Hello Obj inspector</h1>
            </v-card>
          </v-tabs-content>
        </v-tabs-items>
      </v-tabs>
      <v-speed-dial v-model='fab' hover fixed bottom right direction='top'>
        <v-btn slot='activator' fab v-model='fab' color='light-blue'>
          <v-icon>add</v-icon>
          <v-icon>close</v-icon>
        </v-btn>
        <v-btn fab dark small color='cyan' @click='addReceiver'>
          <v-icon>cloud_download</v-icon>
        </v-btn>
        <v-btn fab dark small color='grey' @click='purgeClients'>
          <v-icon>refresh</v-icon>
        </v-btn>
      </v-speed-dial>
    </v-app>
  </div>
</template>

<script>
import AccountsManager from './components/AccountsManager.vue'
import ClientManager from './components/ClientManager.vue'
import { EventBus } from './event-bus'

export default {
  name: 'app',
  components: {
    AccountsManager,
    ClientManager
  },
  data () {
    return {
      fab: {}
    }
  },
  methods: {
    showDev( ) {
      Interop.showDev()
    },
    addReceiver() {
      EventBus.$emit('show-add-receiver-dialog')
    },
    saveClients() {
      Interop.saveFileClients()
    },
    readClients() {
      Interop.getFileStreams()
    },
    purgeClients() {
      Interop.removeAllClients()
      .then( res => {})
      .catch( res => {})
    }
  }
}
</script>

<style>
body{
}
#app {
}
</style>
