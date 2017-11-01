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
        </v-toolbar>
        <v-tabs-bar class='blue white--text' fixed top style='margin-top:50px;'>
          <v-tabs-item key='streams' href='streams'>
            streams
          </v-tabs-item>
          <v-tabs-item key='accounts' href='accounts'>
            accounts
          </v-tabs-item>
          <v-tabs-slider color='white'></v-tabs-slider>
        </v-tabs-bar>
        <v-tabs-items>
          <v-tabs-content id='streams' key='streams'>
             <v-card flat>
              <v-card-text>
                <h1>Hello Future Streams</h1>
              </v-card-text>
            </v-card>
          </v-tabs-content>        
          <v-tabs-content id='accounts' key='accounts' dark>
              <accounts-manager></accounts-manager>
          </v-tabs-content>
        </v-tabs-items>
      </v-tabs>
    </v-app>
  </div>
</template>

<script>
import AccountsManager from './components/AccountsManager.vue'

export default {
  name: 'app',
  components: {
    AccountsManager
  },
  data () {
    return {
      
    }
  },
  methods: {
    showDev( ) {
      Interop.showDev()
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
